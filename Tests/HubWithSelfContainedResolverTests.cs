using System.Threading;
using InteractR.Interactor;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests
{
    [TestFixture]
    public class HubWithSelfContainedResolverTests
    {
        private IInteractorHub _interactorHub;
        private SelfContainedResolver _handlerResolver;
        private IInteractor<MockUseCase, IMockOutputPort> _mockUseCaseInteractor;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = new SelfContainedResolver();
            _interactorHub = new Hub(_handlerResolver);
        }

        [Test]
        public void TestQueryDispatcher()
        {
            _mockUseCaseInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.Register(_mockUseCaseInteractor);
            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort(), CancellationToken.None);
            _mockUseCaseInteractor.Received().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
        }
    }
}