using System;
using System.Collections.Generic;
using InteractorHub.Flow;
using InteractorHub.Notification;

namespace InteractorHub.Resolver
{
    public interface IResolver
    {
        TInteractor ResolveInteractor<TInteractor>();
        object ResolveInteractor(Type interactorType);
        IEnumerable<INotificationListener<TNotification>> ResolveListeners<TNotification>()
            where TNotification : INotification;
        IEnumerable<IFlowController<TRequest>> ResolveFlowController<TRequest>();
    }
}
