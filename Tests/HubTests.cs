using System.Collections.Generic;
using System.Threading;
using InteractorHub.Interactor;
using InteractorHub.Resolver;
using InteractorHub.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractorHub.Tests
{
    [TestFixture]
    public class HubTests
    {
        private IInteractorHub _useCaseMediator;
        private IResolver _handlerResolver;
        private IInteractor<MockInteractionRequest, MockResponse> _mockInteractor;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = Substitute.For<IResolver>();
            _useCaseMediator = new Hub(_handlerResolver);
        }

        [Test]
        public void TestQueryDispatcher()
        {
            _mockInteractor = Substitute.For<IInteractor<MockInteractionRequest, MockResponse>>();
            _handlerResolver.ResolveInteractor<IInteractor<MockInteractionRequest, MockResponse>>().Returns(_mockInteractor);

            _useCaseMediator.Execute<MockResponse, MockInteractionRequest>(new MockInteractionRequest());
            _mockInteractor.Received().Handle(Arg.Any<MockInteractionRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void TestNotificationListener_Is_Triggered()
        {

        }
    }
}