using InteractR.Exceptions;
using InteractR.Interactor;
using InteractR.Resolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public sealed class Hub : IInteractorHub, INotificationIngress
{
    private readonly IResolver _resolver;
    private readonly INotificationHandlingStore _notificationHandlingStore;

    public Hub(IResolver resolver)
        : this(resolver, new InMemoryNotificationHandlingStore())
    {
    }

    public Hub(IResolver resolver, INotificationHandlingStore notificationHandlingStore)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _notificationHandlingStore = notificationHandlingStore ?? throw new ArgumentNullException(nameof(notificationHandlingStore));
    }

    public Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort)
        where TUseCase : IUseCase<TOutputPort>
        => Execute(useCase, outputPort, CancellationToken.None);

    public Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken)
        where TUseCase : IUseCase<TOutputPort>
    {
        if (useCase == null)
        {
            throw new UseCaseNullException("The usecase cannot be null");
        }

        if (outputPort == null)
        {
            throw new OutputPortNullException("The output port cannot be null");
        }

        var interactor = _resolver.ResolveInteractor<TUseCase, TOutputPort>(useCase);
        var pipeline = new List<IMiddleware<TUseCase, TOutputPort>>();

        pipeline
            .AddRange(_resolver.ResolveGlobalMiddleware().Select(x => new GlobalMiddlewareWrapper<TUseCase, TOutputPort>(x)));
        pipeline
            .AddRange(_resolver.ResolveMiddleware<TUseCase>().Select(x => new MiddlewareWrapper<TUseCase, TOutputPort>(x)));
        pipeline
            .AddRange(_resolver.ResolveMiddleware<TUseCase, TOutputPort>(useCase).ToList());

        if (pipeline.Count == 0)
        {
            return interactor.Execute(useCase, outputPort, cancellationToken);
        }

        pipeline = pipeline
            .OrderBy(x => x is IOrderedMiddleware orderedMiddleware ? orderedMiddleware.Order : 0)
            .ToList();

        pipeline.Add(new InteractorMiddlewareWrapper<TUseCase, TOutputPort>(interactor));

        var currentMiddleWare = 0;
        Task<UseCaseResult> NextMiddleWare(TUseCase usecase)
        {
            while (currentMiddleWare < pipeline.Count)
            {
                var middleware = pipeline[currentMiddleWare++];
                if (middleware is IConditionalMiddleware<TUseCase> conditionalMiddleware && !conditionalMiddleware.ShouldExecute(usecase))
                {
                    continue;
                }

                return middleware.Execute(usecase, outputPort, NextMiddleWare, cancellationToken);
            }

            return Task.FromResult(new UseCaseResult(true));
        }

        return NextMiddleWare(useCase);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default, PublishStrategy strategy = PublishStrategy.Sequential)
        where TNotification : INotification
        => Publish(new PublishedNotification<TNotification>(notification), cancellationToken, strategy);

    public Task Ingest<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken = default,
        PublishStrategy strategy = PublishStrategy.Sequential)
        where TNotification : INotification
    {
        if (notification == null)
        {
            throw new UseCaseNullException("The published notification cannot be null");
        }

        var outboundNotification = notification.Origin == NotificationOrigin.OutOfProcess
            ? notification
            : new PublishedNotification<TNotification>(notification.Notification, notification.MessageId, NotificationOrigin.OutOfProcess);

        return Publish(outboundNotification, cancellationToken, strategy);
    }

    public Task StartNotificationInlets(CancellationToken cancellationToken = default, PublishStrategy strategy = PublishStrategy.Sequential)
    {
        var inlets = _resolver.ResolveNotificationInlets();
        if (inlets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(inlets.Select(x => x.Start(this, cancellationToken, strategy)));
    }

    public async Task Publish<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken = default,
        PublishStrategy strategy = PublishStrategy.Sequential)
        where TNotification : INotification
    {
        if (notification == null)
        {
            throw new UseCaseNullException("The published notification cannot be null");
        }

        var notificationKey = GetNotificationKey<TNotification>(notification.MessageId);
        if (!await _notificationHandlingStore.TryMarkAsHandled(notificationKey, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await DispatchInProcess(notification.Notification, cancellationToken, strategy).ConfigureAwait(false);

            if (notification.Origin == NotificationOrigin.InProcess)
            {
                await DispatchOutOfProcess(notification, cancellationToken, strategy).ConfigureAwait(false);
            }
        }
        catch
        {
            await _notificationHandlingStore.UnmarkAsHandled(notificationKey, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort> => Execute(useCase, outputPort);

    public Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort> => Execute(useCase, outputPort, cancellationToken);

    private static async Task PublishSequential<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken,
        IReadOnlyList<INotificationHandler<TNotification>> handlers)
        where TNotification : INotification
    {
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task DispatchInProcess<TNotification>(TNotification notification, CancellationToken cancellationToken, PublishStrategy strategy)
        where TNotification : INotification
    {
        var handlers = _resolver.ResolveNotificationHandlers<TNotification>();
        if (handlers.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (strategy == PublishStrategy.Parallel)
        {
            return Task.WhenAll(handlers.Select(x => x.Handle(notification, cancellationToken)));
        }

        return PublishSequential(notification, cancellationToken, handlers);
    }

    private Task DispatchOutOfProcess<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken, PublishStrategy strategy)
        where TNotification : INotification
    {
        var outlets = _resolver.ResolveNotificationOutlets();
        if (outlets.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (strategy == PublishStrategy.Parallel)
        {
            return Task.WhenAll(outlets.Select(x => x.Publish(notification, cancellationToken)));
        }

        return PublishToOutletsSequential(notification, cancellationToken, outlets);
    }

    private static async Task PublishToOutletsSequential<TNotification>(PublishedNotification<TNotification> notification,
        CancellationToken cancellationToken,
        IReadOnlyList<INotificationOutlet> outlets)
        where TNotification : INotification
    {
        foreach (var outlet in outlets)
        {
            await outlet.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetNotificationKey<TNotification>(string messageId)
        where TNotification : INotification
        => $"{typeof(TNotification).FullName}:{messageId}";
}