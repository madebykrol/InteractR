using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor
{
    internal sealed class GlobalMiddlewareWrapper<TUseCase, TOutputPort> : IMiddleware<TUseCase, TOutputPort> where TUseCase : IUseCase<TOutputPort>
    {
        private IMiddleware _middleware;

        public GlobalMiddlewareWrapper(IMiddleware middleware)
        {
            _middleware = middleware;
        }
        public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken) 
            => _middleware.Execute(usecase, next, cancellationToken);
    }
}
