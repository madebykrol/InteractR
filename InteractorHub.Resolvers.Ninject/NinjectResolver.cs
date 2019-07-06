using System;
using System.Collections.Generic;
using InteractorHub.Notification;
using InteractorHub.Pipeline;
using InteractorHub.Resolver;
using Ninject;

namespace InteractorHub.Resolvers.Ninject
{
    public class NinjectResolver : IResolver
    {
        private readonly IKernel _kernel;

        public NinjectResolver(IKernel kernel)
        {
            _kernel = kernel;
        } 

        public TInteractor ResolveInteractor<TInteractor>()
        {
            return Resolve<TInteractor>();
        }

        public object ResolveInteractor(Type interactorType)
        {
            return Resolve(interactorType);
        }

        public IEnumerable<INotificationListener<TNotification>> ResolveListeners<TNotification>() where TNotification : INotification
        {
            var notificationListeners = ResolveMultiple<INotificationListener<TNotification>>();

            return notificationListeners;
        }

        public IEnumerable<IPreInteractionMiddleware<TRequest>> ResolvePreInteractionMiddleWare<TRequest>()
        {
            throw new NotImplementedException();
        }

        private T Resolve<T>()
        {
            return _kernel.Get<T>();
        }

        private object Resolve(Type t)
        {
            return _kernel.Get(t);
        }

        private IEnumerable<T> ResolveMultiple<T>()
        {
            return _kernel.GetAll<T>();
        }
    }
}
