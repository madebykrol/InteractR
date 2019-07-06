using System;
using System.Collections.Generic;
using System.Text;
using InteractorHub.Flow;
using InteractorHub.Interactor;
using InteractorHub.Notification;

namespace InteractorHub.Resolver
{
    public interface IRegistrator
    {
        void Register<TRequest>(IFlowController<TRequest> flowController);

        void Register<TNotification>(INotificationListener<TNotification> notificationListener)
            where TNotification : INotification;

        void Register<TRequest, TResponse>(IInteractor<TRequest, TResponse> interactor)
            where TRequest : IInteractionRequest<TResponse>;
    }
}
