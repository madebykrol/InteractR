using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UseCaseMediator.Interactor;

namespace UseCaseMediator.Tests.Mocks
{
    public class MockUseCaseInteractor : IUseCaseInteractor<MockUseCaseRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockUseCaseRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse() {HasBeenHandled = true};
        }
    }
}
