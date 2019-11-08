using System;
using System.Collections.Generic;
using InteractR.Interactor;

namespace InteractR.Resolver
{
    public sealed class SelfContainedResolver : IResolver, IRegistrator
    { 
        private readonly Dictionary<Type, object> _interactors = new Dictionary<Type, object>();

        public IInteractor<TUseCase, TOutputPort> ResolveInteractor<TUseCase, TOutputPort>(TUseCase useCase) where TUseCase : IUseCase<TOutputPort>
        {
            return (IInteractor<TUseCase, TOutputPort>)ResolveInteractor(typeof(IInteractor<TUseCase, TOutputPort>));
        }

        private object ResolveInteractor(Type interactorType)
        {
            return !_interactors.ContainsKey(interactorType) 
                ? null 
                : _interactors[interactorType];
        }

        public void Register<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor) where TUseCase : IUseCase<TOutputPort>
        {
            if (_interactors.ContainsKey(typeof(IInteractor<TUseCase, TOutputPort>)))
                return;

            _interactors.Add(typeof(IInteractor<TUseCase, TOutputPort>), interactor);
        }

    }
}
