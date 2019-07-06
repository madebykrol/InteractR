using System;
using System.Collections.Generic;
using Autofac;
using InteractorHub.Flow;
using InteractorHub.Notification;
using InteractorHub.Resolver;

namespace InteractorHub.Resolvers.AutoFac
{
    public class AutoFacResolver : IResolver
    {
        private readonly IContainer _container;
        public AutoFacResolver(IContainer container)
        {
            _container = container;
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
            return Resolve<IEnumerable<INotificationListener<TNotification>>>();
        }

        public IEnumerable<IFlowController<TRequest>> ResolveFlowController<TRequest>()
        {
            return Resolve<IEnumerable<IFlowController<TRequest>>>();
        }

        private T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        private object Resolve(Type t)
        {
            return _container.Resolve(t);
        }
    }
}
