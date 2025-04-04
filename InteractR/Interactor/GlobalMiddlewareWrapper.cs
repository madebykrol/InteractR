using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class GlobalMiddlewareWrapper<TUseCaseIn>
    : IMiddleware<TUseCaseIn>
{
    private readonly IMiddleware _middleware;

    public GlobalMiddlewareWrapper(IMiddleware middleware)
    {;
        _middleware = middleware;
    }

    public Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
    where TUseCase : TUseCaseIn
        => _middleware.Execute(usecase, next, cancellationToken);
}