using System;
using System.Collections.Generic;
using InteractR.Interactor;

namespace InteractR.Resolver
{
    public sealed class SelfContainedResolver : IResolver, IRegistrator
    { 
        private readonly Dictionary<Type, object> _interactors = new Dictionary<Type, object>();
        private readonly Dictionary<Type, IList<object>> _pipeline = new Dictionary<Type, IList<object>>();
        private readonly IList<IMiddleware> _globalPipeline = new List<IMiddleware>();

        public IInteractor<TUseCase, TOutputPort> ResolveInteractor<TUseCase, TOutputPort>(TUseCase useCase) where TUseCase : IUseCase<TOutputPort> 
            => (IInteractor<TUseCase, TOutputPort>)ResolveInteractor(typeof(IInteractor<TUseCase, TOutputPort>));

        private object ResolveInteractor(Type interactorType) =>
            _interactors.ContainsKey(interactorType)
                ? _interactors[interactorType]
                : null;

        public IReadOnlyList<IMiddleware<TUseCase, TOutputPort>> ResolveMiddleware<TUseCase, TOutputPort>(TUseCase useCase) where TUseCase : IUseCase<TOutputPort> 
            => (IReadOnlyList<IMiddleware<TUseCase, TOutputPort>>)ResolveMiddleware(typeof(TUseCase)) ?? new List<IMiddleware<TUseCase, TOutputPort>>();

        public IReadOnlyList<IMiddleware> ResolveGlobalMiddleware() => (IReadOnlyList<IMiddleware>)_globalPipeline ?? new List<IMiddleware>();

        private object ResolveMiddleware(Type useCase) =>
            _pipeline.ContainsKey(useCase)
                ? _pipeline[useCase]
                : null;

        public void Register<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor) where TUseCase : IUseCase<TOutputPort>
        {
            if (_interactors.ContainsKey(typeof(IInteractor<TUseCase, TOutputPort>)))
                return;

            _interactors.Add(typeof(IInteractor<TUseCase, TOutputPort>), interactor);
        }

        public void Register<TUseCase, TOutputPort>(IMiddleware<TUseCase, TOutputPort> middleware) where TUseCase : IUseCase<TOutputPort>
        {
            var useCaseType = typeof(TUseCase);
            if(!_pipeline.ContainsKey(useCaseType))
                _pipeline[useCaseType] = new List<object>();

            _pipeline[useCaseType].Add(middleware);
        }

        public void Register(IMiddleware middleware)
        {
            _globalPipeline.Add(middleware);
        }
    }
}
