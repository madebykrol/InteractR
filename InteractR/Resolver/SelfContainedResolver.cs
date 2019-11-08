using System;
using System.Collections.Generic;
using System.Linq;
using InteractorHub.Interactor;

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

        public void Register<TRequest, TResponse>(IInteractor<TRequest, TResponse> interactor) where TRequest : IInteractionRequest<TResponse>
        {
            if (_interactors.ContainsKey(typeof(IInteractor<TRequest, TResponse>)))
                return;

            _interactors.Add(typeof(IInteractor<TRequest, TResponse>), interactor);
        }
    }
}
