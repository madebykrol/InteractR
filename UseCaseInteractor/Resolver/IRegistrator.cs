using System;
using System.Collections.Generic;
using System.Text;
using UseCaseMediator.Flow;
using UseCaseMediator.Interactor;
using UseCaseMediator.Notification;

namespace UseCaseMediator.Resolver
{
    internal interface IRegistrator
    {
        void Register<TRequest>(IFlowController<TRequest> flowController);
        void Register<TNotification>(INotificationListener<TNotification> notificationListener) where TNotification : INotification;
        void Register<TRequest, TResponse>(IUseCaseInteractor<TRequest, TResponse> interactor) where TRequest : IUseCaseRequest<TResponse>;
    }
}
