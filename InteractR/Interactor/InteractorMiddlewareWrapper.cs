using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class InteractorMiddlewareWrapper<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor)
    : IMiddleware<TUseCase, TOutputPort>, IOrderedMiddleware, IConditionalMiddleware<TUseCase>
    where TUseCase : IUseCase<TOutputPort>
{
    public int Order => int.MaxValue;

    public bool ShouldExecute(TUseCase usecase) => true;

    public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken) 
        => interactor.Execute(usecase, outputPort, cancellationToken);
}