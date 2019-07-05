namespace InteractorHub.Tests.Mocks
{
    public class MockUseCaseInteractor : IUseCaseInteractor<MockUseCaseRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockUseCaseRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse() {HasBeenHandled = true};
        }
    }
}
