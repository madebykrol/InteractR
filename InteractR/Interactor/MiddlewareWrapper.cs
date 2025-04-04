using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class MiddlewareWrapper<TUseCaseIn>
    : IMiddleware<TUseCaseIn>
{
    private readonly IMiddleware<TUseCaseIn> _middleware;

    public MiddlewareWrapper(IMiddleware<TUseCaseIn> middleware)
    {
        _middleware = middleware;
    }

    public Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        where TUseCase : TUseCaseIn 
        => _middleware.Execute(usecase, next, cancellationToken);
}