using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor
{
    internal sealed class InteractorMiddlewareWrapper<TUseCase, TOutputPort> : IMiddleware<TUseCase, TOutputPort>
        where TUseCase : IUseCase<TOutputPort>
    {
        private readonly IInteractor<TUseCase, TOutputPort> _interactor;

        public InteractorMiddlewareWrapper(IInteractor<TUseCase, TOutputPort> interactor)
        {
            _interactor = interactor;
        }

        public Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        {
            return _interactor.Execute(usecase, outputPort, cancellationToken);
        }
    }
}
