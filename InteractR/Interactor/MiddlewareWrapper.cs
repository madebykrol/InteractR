using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor
{
    internal sealed class MiddlewareWrapper<TUseCase, TOutputPort> : IMiddleware<TUseCase, TOutputPort> where TUseCase : IUseCase<TOutputPort>
    {
        private readonly IMiddleware<TUseCase> _middleware;

        public MiddlewareWrapper(IMiddleware<TUseCase> middleware)
        {
            _middleware = middleware;
        }

        public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        {
            return _middleware.Execute(usecase, next, cancellationToken);
        }
    }
}
