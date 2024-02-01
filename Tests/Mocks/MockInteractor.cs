using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;

namespace InteractR.Tests.Mocks;

public class MockInteractor : IInteractor<MockUseCase, IMockOutputPort>
{
    public Task<UseCaseResult> Execute(MockUseCase usecase, IMockOutputPort outputPort, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UseCaseResult(true));
    }
}