using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;

namespace InteractR.Tests.Mocks
{
    public class MockGlobalMiddleware : IMiddleware
    {
        public Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        {
            return next.Invoke(usecase);
        }
    }
}
