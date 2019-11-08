using System;
using InteractR.Interactor;

namespace InteractR.Resolver
{
    public interface IResolver
    {
        IInteractor<TUseCase, TOutputPort> ResolveInteractor<TUseCase, TOutputPort>(TUseCase useCase)
            where TUseCase : IUseCase<TOutputPort>;
    }
}
