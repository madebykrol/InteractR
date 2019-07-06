using System;
using System.Collections.Generic;
using System.Text;
using InteractorHub.Interactor;
using InteractorHub.Notification;
using InteractorHub.Pipeline;

namespace InteractorHub.Resolver
{
    public interface IRegistrator
    {
        void Register<TRequest>(IPreInteractionMiddleware<TRequest> preInteractionMiddleware);

        void Register<TNotification>(INotificationListener<TNotification> notificationListener)
            where TNotification : INotification;

        void Register<TRequest, TResponse>(IInteractor<TRequest, TResponse> interactor)
            where TRequest : IInteractionRequest<TResponse>;
    }
}
