using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class GlobalMiddlewareWrapper<TUseCase, TOutputPort>
    : IMiddleware<TUseCase, TOutputPort>, IOrderedMiddleware, IConditionalMiddleware<TUseCase>
    where TUseCase : IUseCase<TOutputPort>
{
    private readonly IMiddleware _middleware;

    public GlobalMiddlewareWrapper(IMiddleware middleware)
    {
        _middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
    }

    public int Order => _middleware is IOrderedMiddleware orderedMiddleware
        ? orderedMiddleware.Order
        : 0;

    public bool ShouldExecute(TUseCase usecase) => _middleware is IConditionalMiddleware<TUseCase> conditionalMiddleware
        ? conditionalMiddleware.ShouldExecute(usecase)
        : true;

    public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken) 
        => _middleware.Execute(usecase, next, cancellationToken);
}