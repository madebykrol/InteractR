using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;

namespace InteractR.Tests.Mocks;

public class MockMiddleware : IMiddleware<IHasPolicy>
{
    private const string UnauthorizedFailureCode = "DERP";
    public async Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        where TUseCase : IHasPolicy
    {
        return await next(usecase);
    }
}