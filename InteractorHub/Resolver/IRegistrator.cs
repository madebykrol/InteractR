using InteractorHub.Flow;
using InteractorHub.Interactor;
using InteractorHub.Notification;

namespace InteractorHub.Resolver
{
    internal interface IRegistrator
    {
        void Register<TRequest>(IFlowController<TRequest> flowController);
        void Register<TNotification>(INotificationListener<TNotification> notificationListener) where TNotification : INotification;
        void Register<TRequest, TResponse>(IInteractor<TRequest, TResponse> interactor) where TRequest : IUseCaseRequest<TResponse>;
    }
}
