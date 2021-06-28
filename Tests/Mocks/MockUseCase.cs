using InteractR.Interactor;

namespace InteractR.Tests.Mocks
{
    public class MockUseCase : IUseCase<IMockOutputPort>, IHasPolicy
    {
        public string Policy { get; } = "MockPolicy";
    }
}
