using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class MiddlewareWrapper<TUseCase, TOutputPort>(IMiddleware<TUseCase> middleware)
    : IMiddleware<TUseCase, TOutputPort>, IOrdered, IConditionalMiddleware<TUseCase>
    where TUseCase : IUseCase<TOutputPort>
{
    public int Order => middleware is IOrdered orderedMiddleware
        ? orderedMiddleware.Order
        : 0;

    public bool ShouldExecute(TUseCase usecase) => middleware is IConditionalMiddleware<TUseCase> conditionalMiddleware
        ? conditionalMiddleware.ShouldExecute(usecase)
        : true;

    public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, CancellationToken?, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        => middleware.Execute(usecase, next, cancellationToken);
}