using System.Threading;
using InteractR.Interactor;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests
{
    [TestFixture]
    public class HubTests
    {
        private IInteractorHub _interactorHub;
        private IResolver _handlerResolver;
        private IInteractor<MockUseCase, IMockOutputPort> _mockInteractor;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = Substitute.For<IResolver>();
            _interactorHub = new Hub(_handlerResolver);
        }

        [Test]
        public void UseCaseDispatcher()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());
            _mockInteractor.Received().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
        }
    }
}