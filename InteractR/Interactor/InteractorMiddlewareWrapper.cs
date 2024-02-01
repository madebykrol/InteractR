using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class InteractorMiddlewareWrapper<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor)
    : IMiddleware<TUseCase, TOutputPort>
    where TUseCase : IUseCase<TOutputPort>
{
    public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken) 
        => interactor.Execute(usecase, outputPort, cancellationToken);
}