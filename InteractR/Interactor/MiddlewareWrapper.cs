using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class MiddlewareWrapper<TUseCase, TOutputPort>(IMiddleware<TUseCase> middleware)
    : IMiddleware<TUseCase, TOutputPort>
    where TUseCase : IUseCase<TOutputPort>
{
    public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        => middleware.Execute(usecase, next, cancellationToken);
}