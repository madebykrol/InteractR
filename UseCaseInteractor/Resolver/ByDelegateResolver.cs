using System;
using System.Collections.Generic;
using UseCaseMediator.Flow;
using UseCaseMediator.Notification;

namespace UseCaseMediator.Resolver
{
    public class ByDelegateResolver : IResolver
    {
        private readonly Func<Type, object> _resolveFunc;

        public ByDelegateResolver(Func<Type, object> resolveFunc)
        {
            _resolveFunc = resolveFunc;
        }
        public TInteractor ResolveInteractor<TInteractor>()
        {
            return (TInteractor)ResolveInteractor(typeof(TInteractor));
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
            return (T)Resolve(typeof(T));
        }

        private object Resolve(Type t)
        {
            return _resolveFunc(t);
        }
    }
}
