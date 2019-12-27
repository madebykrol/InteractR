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
        private readonly TOutputPort _outputPort;

        public InteractorMiddlewareWrapper(IInteractor<TUseCase, TOutputPort> interactor, TOutputPort outputPort)
        {
            _interactor = interactor;
            _outputPort = outputPort;
        }

        public Task<UseCaseResult> Execute(TUseCase usecase, IMiddleware<TUseCase, TOutputPort> next, CancellationToken cancellationToken)
        {
            return _interactor.Execute(usecase, _outputPort, cancellationToken);
        }
    }
}
