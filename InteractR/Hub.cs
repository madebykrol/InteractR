using InteractR.Exceptions;
using InteractR.Interactor;
using InteractR.Notifications;
using InteractR.Resolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public class Hub : IHub
{
    private readonly IResolver _resolver;
    private readonly INotificationTypeRegistry _typeRegistry;
    private readonly IHubOptions _options;

    public Hub(IResolver resolver) : this(resolver, new NotificationTypeRegistry(), new HubOptions())
    {
    }

    public Hub(IResolver resolver, INotificationTypeRegistry typeRegistry, IHubOptions hubOptions)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        _options = hubOptions ?? throw new ArgumentNullException(nameof(hubOptions));
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
            .OrderBy(x => x is IOrdered orderedMiddleware ? orderedMiddleware.Order : 0)
            .ToList();

        pipeline.Add(new InteractorMiddlewareWrapper<TUseCase, TOutputPort>(interactor));

        var currentMiddleWare = 0;
        Task<UseCaseResult> NextMiddleWare(TUseCase usecase, CancellationToken? cancellationTokenOverride = null)
        {
            var effectiveCancellationToken = cancellationTokenOverride ?? cancellationToken;
            while (currentMiddleWare < pipeline.Count)
            {
                var middleware = pipeline[currentMiddleWare++];
                if (middleware is IConditionalMiddleware<TUseCase> conditionalMiddleware && !conditionalMiddleware.ShouldExecute(usecase))
                {
                    continue;
                }

                return middleware.Execute(usecase, outputPort, NextMiddleWare, effectiveCancellationToken);
            }

            return Task.FromResult(new UseCaseResult(true));
        }

        return NextMiddleWare(useCase);
    }

    public Task OpenNotificationInlets(CancellationToken cancellationToken = default, EProcessingStrategy strategy = EProcessingStrategy.Sequential)
    {
        var inlets = _resolver.ResolveNotificationInlets();
        if (inlets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            inlets.Select(x => 
                x.Open((envelope) => SendEnvelopeToHandlers(envelope, x, cancellationToken),
                    cancellationToken,
                    strategy)));
    }

    protected Type GetNotificationTypeFromEnvelope(NotificationEnvelope envelope)
    {
        return _typeRegistry.Resolve(envelope.Subject, envelope.Topic);
    }

    protected async Task<EInletResponse> SendEnvelopeToHandlers(NotificationEnvelope envelope, INotificationInlet inlet, CancellationToken cancellationToken)
    {
        try
        {
            var notificationType = GetNotificationTypeFromEnvelope(envelope);
            if (notificationType == null)
                return EInletResponse.Nack;

            var notification = System.Text.Json.JsonSerializer.Deserialize(envelope.Payload, notificationType);

            var resolveMethod = typeof(IResolver).GetMethod(nameof(IResolver.ResolveNotificationHandlers))
                ?.MakeGenericMethod(notificationType);
            if (resolveMethod == null)
                return EInletResponse.Nack;

            var handlers = resolveMethod.Invoke(_resolver, Array.Empty<object>());

            var publishMethod = typeof(Hub).GetMethod(nameof(PublishSequential), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(notificationType);
            if (publishMethod == null)
                return EInletResponse.Nack;

            var result = await (Task<ENotificationResponse>)publishMethod.Invoke(null, new object[] { notification, cancellationToken, handlers });

            return result == ENotificationResponse.Failed ? EInletResponse.Nack : EInletResponse.Ack;
        }
        catch (Exception)
        {
            // On any failure, Nack the message so the broker can retry or dead-letter it
            return EInletResponse.Nack;
        }
    }

    public Task OpenNotificationOutlets(CancellationToken cancellationToken = default)
    {
        var outlets = _resolver.ResolveNotificationOutlets();

        if (outlets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(outlets.Select(x => x.Open(cancellationToken)));
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        => PublishInternal(notification, Guid.NewGuid().ToString(), cancellationToken);

    public Task Publish<TNotification>(TNotification notification, string messageId, CancellationToken cancellationToken = default)
        => PublishInternal(notification, messageId, cancellationToken);

    private async Task PublishInternal<TNotification>(TNotification notification, string messageId, CancellationToken cancellationToken)
    {
        var handlers = _resolver.ResolveNotificationHandlers<TNotification>();
        await PublishSequential(notification, cancellationToken, handlers);

        var address = _typeRegistry.ResolveAddress(typeof(TNotification));
        var envelope = new NotificationEnvelope
        {
            MessageId = messageId,
            Subject = address.Subject,
            Topic = address.Topic,
            Payload = System.Text.Json.JsonSerializer.Serialize(notification),
        };

        var outlets = _resolver.ResolveNotificationOutlets();
        foreach (var outlet in outlets)
        {
            await outlet.Publish(envelope, cancellationToken);
        }
    }


    public Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort> => Execute(useCase, outputPort);

    public Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort> => Execute(useCase, outputPort, cancellationToken);

    private static async Task<ENotificationResponse> PublishSequential<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken,
        IReadOnlyList<INotificationHandler<TNotification>> handlers)
    {
        if (handlers.Count == 0)
        {
            return ENotificationResponse.Ignored;
        }

        var overallResult = ENotificationResponse.Completed;
        foreach (var handler in handlers)
        {
            var result = await handler.Handle(notification, cancellationToken).ConfigureAwait(false);

            // If any handler fails, mark overall as failed
            if (result == ENotificationResponse.Failed)
            {
                overallResult = ENotificationResponse.Failed;
            }
        }

        return overallResult;
    }
}

public interface IHubOptions
{
    Func<object, NotificationRouteData>? ResolveNotificationRouteData { get; set; }
}

public class HubOptions : IHubOptions
{
    public Func<object, NotificationRouteData>? ResolveNotificationRouteData { get; set; }
}

public class NotificationRouteData
{
    private string RouteKey { get; set; }
    private string Topic { get; set; }

}