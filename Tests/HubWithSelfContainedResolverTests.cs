using System.Threading;
using InteractorHub.Interactor;
using InteractorHub.Resolver;
using InteractorHub.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractorHub.Tests
{
    [TestFixture]
    public class HubWithSelfContainedResolverTests
    {
        private IInteractorHub _useCaseMediator;
        private SelfContainedResolver _handlerResolver;
        private IInteractor<MockInteractionRequest, MockResponse> _mockUseCaseInteractor;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = new SelfContainedResolver();
            _useCaseMediator = new Hub(_handlerResolver);
        }

        [Test]
        public void TestQueryDispatcher()
        {
            _mockUseCaseInteractor = Substitute.For<IInteractor<MockInteractionRequest, MockResponse>>();
            _handlerResolver.Register(_mockUseCaseInteractor);
            _useCaseMediator.Execute<MockResponse, MockInteractionRequest>(new MockInteractionRequest());
            _mockUseCaseInteractor.Received().Handle(Arg.Any<MockInteractionRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void TestNotificationListener_Is_Triggered()
        {
        }
    }
}