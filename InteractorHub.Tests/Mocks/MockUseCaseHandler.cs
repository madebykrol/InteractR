namespace InteractorHub.Tests.Mocks
{
    public class MockUseCaseInteractor : IUseCaseInteractor<MockInteractionRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockInteractionRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse() {HasBeenHandled = true};
        }
    }
}
