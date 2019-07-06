using System;
using System.Collections.Generic;
using System.Linq;
using InteractorHub.Flow;
using InteractorHub.Interactor;
using InteractorHub.Notification;

namespace InteractorHub.Resolver
{
    public sealed class SelfContainedResolver : IResolver, IRegistrator
    { 
        private readonly Dictionary<Type, object> _interactors = new Dictionary<Type, object>();
        private readonly Dictionary<Type, List<object>> _flowControllers = new Dictionary<Type, List<object>>();
        private readonly Dictionary<Type, List<object>> _notificationListeners = new Dictionary<Type, List<object>>();

        public TInteractor ResolveInteractor<TInteractor>()
        {
            return (TInteractor) ResolveInteractor(typeof(TInteractor));
        }

        public object ResolveInteractor(Type interactorType)
        {
            return !_interactors.ContainsKey(interactorType) 
                ? null 
                : _interactors[interactorType];
        }

        public IEnumerable<INotificationListener<TNotification>> ResolveListeners<TNotification>() where TNotification : INotification
        {
            return !_notificationListeners.ContainsKey(typeof(TNotification)) 
                ? new List<INotificationListener<TNotification>>() 
                : _notificationListeners[typeof(TNotification)].Select(x => (INotificationListener<TNotification>) x);
        }

        public IEnumerable<IFlowController<TRequest>> ResolveFlowController<TRequest>()
        {
            return !_flowControllers.ContainsKey(typeof(TRequest))
                ? new List<IFlowController<TRequest>>() : _notificationListeners[typeof(TRequest)].Select(x => (IFlowController<TRequest>)x);
        }

        public void Register<TRequest>(IFlowController<TRequest> flowController)
        {
            if (!_flowControllers.ContainsKey(typeof(TRequest)))
            {
                _flowControllers.Add(typeof(TRequest), new List<object>());
            }

            _flowControllers[typeof(TRequest)].Add(flowController);
        }

        public void Register<TNotification>(INotificationListener<TNotification> notificationListener) where TNotification : INotification
        {
            if (!_notificationListeners.ContainsKey(typeof(TNotification)))
            {
                _notificationListeners.Add(typeof(TNotification), new List<object>());
            }

            _notificationListeners[typeof(TNotification)].Add(notificationListener);
        }

        public void Register<TRequest, TResponse>(IInteractor<TRequest, TResponse> interactor) where TRequest : IInteractionRequest<TResponse>
        {
            if (_interactors.ContainsKey(typeof(IInteractor<TRequest, TResponse>)))
                return;

            _interactors.Add(typeof(IInteractor<TRequest, TResponse>), interactor);
        }
    }
}
