using System;
using System.Collections.Generic;
using InteractorHub.Notification;
using InteractorHub.Pipeline;

namespace InteractorHub.Resolver
{
    public interface IResolver
    {
        TInteractor ResolveInteractor<TInteractor>();
        object ResolveInteractor(Type interactorType);
        IEnumerable<INotificationListener<TNotification>> ResolveListeners<TNotification>()
            where TNotification : INotification;
        IEnumerable<IPreInteractionMiddleware<TRequest>> ResolvePreInteractionMiddleWare<TRequest>();
    }
}
