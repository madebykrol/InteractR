using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class MiddlewareWrapper<TUseCase, TOutputPort>(IMiddleware<TUseCase> middleware)
    : IMiddleware<TUseCase, TOutputPort>, IOrderedMiddleware, IConditionalMiddleware<TUseCase>
    where TUseCase : IUseCase<TOutputPort>
{
    public int Order => middleware is IOrderedMiddleware orderedMiddleware
        ? orderedMiddleware.Order
        : 0;

    public bool ShouldExecute(TUseCase usecase) => middleware is IConditionalMiddleware<TUseCase> conditionalMiddleware
        ? conditionalMiddleware.ShouldExecute(usecase)
        : true;

    public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        => middleware.Execute(usecase, next, cancellationToken);
}