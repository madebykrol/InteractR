using System;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;

namespace InteractR.Tests.Mocks
{
    public class MockMiddleware : IMiddleware<IHasPolicy>
    {
        async Task<UseCaseResult> IMiddleware<IHasPolicy>.Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        {
            return await next(usecase);
        }
    }
}
