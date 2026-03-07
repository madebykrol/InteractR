using InteractR;
using InteractR.Interactor;

namespace InteractR.Resolver;

public interface IRegistrator
{
    void Register<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor)
        where TUseCase : IUseCase<TOutputPort>;

    void Register<TUseCase, TOutputPort>(IMiddleware<TUseCase, TOutputPort> middleware)
        where TUseCase : IUseCase<TOutputPort>;

    void Register<TUseCase>(IMiddleware<TUseCase> middleware);

    void Register<TNotification>(INotificationHandler<TNotification> notificationHandler)
        where TNotification : INotification;

    void Register(INotificationInlet inlet);

    void Register(INotificationOutlet outlet);

    void Register(IMiddleware middleware);
}