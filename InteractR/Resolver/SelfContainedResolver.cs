using InteractR.Interactor;
using InteractR.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InteractR.Resolver;

public sealed class SelfContainedResolver : IResolver, IRegistrator
{
    private readonly Dictionary<Type, object> _interactors = new();
    private readonly Dictionary<Type, IList<object>> _pipeline = new();
    private readonly Dictionary<Type, IList<object>> _middleware = new();
    private readonly Dictionary<Type, IList<object>> _notificationHandlers = new();
    private readonly IList<IMiddleware> _globalPipeline = new List<IMiddleware>();
    private readonly IList<INotificationInlet> _notificationInlets = new List<INotificationInlet>();
    private readonly IList<INotificationOutlet> _notificationOutlets = new List<INotificationOutlet>();

    public INotificationTypeRegistry NotificationTypeRegistry { get; } = new NotificationTypeRegistry();

    public IInteractor<TUseCase, TOutputPort> ResolveInteractor<TUseCase, TOutputPort>(TUseCase useCase) where TUseCase : IUseCase<TOutputPort>
        => (IInteractor<TUseCase, TOutputPort>)ResolveInteractor(typeof(IInteractor<TUseCase, TOutputPort>));

    private object ResolveInteractor(Type interactorType)
        => _interactors.FirstOrDefault(x => interactorType.IsAssignableFrom(x.Key)).Value;

    public IReadOnlyList<IMiddleware<TUseCase, TOutputPort>> ResolveMiddleware<TUseCase, TOutputPort>(TUseCase useCase) where TUseCase : IUseCase<TOutputPort> =>
        ResolveMiddleware(typeof(TUseCase))?
            .Select(x => (IMiddleware<TUseCase, TOutputPort>)x).ToList() ?? [];

    public IReadOnlyList<IMiddleware<TUseCase>> ResolveMiddleware<TUseCase>()
    {
        var middlewares = new List<IMiddleware<TUseCase>>();
        var potentialMiddleware = _middleware.Where(x => x.Key.IsAssignableFrom(typeof(TUseCase)));

        foreach (var d in potentialMiddleware)
        {
            middlewares.AddRange(d.Value.Select(x => (IMiddleware<TUseCase>) x));
        }

        return middlewares;
    }

    public IReadOnlyList<IMiddleware> ResolveGlobalMiddleware() => (IReadOnlyList<IMiddleware>)_globalPipeline ?? new List<IMiddleware>();

    public IReadOnlyList<INotificationHandler<TNotification>> ResolveNotificationHandlers<TNotification>()
    {
        var notificationType = typeof(TNotification);
        if (!_notificationHandlers.ContainsKey(notificationType))
        {
            return [];
        }

        return _notificationHandlers[notificationType]
            .Select(x => (INotificationHandler<TNotification>)x)
            .ToList();
    }

    public IReadOnlyList<INotificationOutlet> ResolveNotificationOutlets() => (IReadOnlyList<INotificationOutlet>)_notificationOutlets;

    public IReadOnlyList<INotificationInlet> ResolveNotificationInlets() => (IReadOnlyList<INotificationInlet>)_notificationInlets;

    private IList<object> ResolveMiddleware(Type useCase) =>
        _pipeline.ContainsKey(useCase)
            ? _pipeline[useCase]
            : null;

    public void Register<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor) where TUseCase : IUseCase<TOutputPort>
    {
        if (_interactors.ContainsKey(typeof(IInteractor<TUseCase, TOutputPort>)))
            return;

        _interactors.Add(typeof(IInteractor<TUseCase, TOutputPort>), interactor);
    }

    public void Register<TUseCase, TOutputPort>(IMiddleware<TUseCase, TOutputPort> middleware) where TUseCase : IUseCase<TOutputPort>
    {
        var useCaseType = typeof(TUseCase);
        if (!_pipeline.ContainsKey(useCaseType))
            _pipeline[useCaseType] = new List<object>();

        _pipeline[useCaseType].Add(middleware);
    }

    public void Register<TUseCase>(IMiddleware<TUseCase> middleware)
    {
        var useCaseType = typeof(TUseCase);
        if (!_middleware.ContainsKey(useCaseType))
            _middleware[useCaseType] = new List<object>();

        _middleware[useCaseType].Add(middleware);
    }

    public void Register<TNotification>(INotificationHandler<TNotification> notificationHandler)
    {
        var notificationType = typeof(TNotification);
        if (!_notificationHandlers.ContainsKey(notificationType))
            _notificationHandlers[notificationType] = new List<object>();

        _notificationHandlers[notificationType].Add(notificationHandler);
        NotificationTypeRegistry.RegisterSubscription(notificationType, notificationHandler.GetType());
    }

    public void Register(INotificationOutlet outlet)
    {
        _notificationOutlets.Add(outlet);
    }

    public void Register(INotificationInlet inlet)
    {
        _notificationInlets.Add(inlet);
    }

    public void Register(IMiddleware middleware)
    {
        _globalPipeline.Add(middleware);
    }
}