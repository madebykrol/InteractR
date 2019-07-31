using System.Threading;
using System.Threading.Tasks;
using InteractorHub.Interactor;

namespace InteractorHub.Tests.Mocks
{
    public class MockUseCaseInteractor : IInteractor<MockInteractionRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockInteractionRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse() {HasBeenHandled = true};
        }
    }
}
