using System.Collections.Generic;
using System.Threading;
using InteractorHub.Interactor;
using InteractorHub.Notification;
using InteractorHub.Resolver;
using InteractorHub.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractorHub.Tests
{
    [TestFixture]
    public class HubTests
    {
        private IHub _useCaseMediator;
        private IResolver _handlerResolver;
        private IInteractor<MockUseCaseRequest, MockResponse> _mockInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = Substitute.For<IResolver>();
            _useCaseMediator = new Hub(_handlerResolver);
        }

        [Test]
        public void TestQueryDispatcher()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCaseRequest, MockResponse>>();
            _handlerResolver.ResolveInteractor<IInteractor<MockUseCaseRequest, MockResponse>>().Returns(_mockInteractor);

            _useCaseMediator.Handle<MockResponse, MockUseCaseRequest>(new MockUseCaseRequest());
            _mockInteractor.Received().Handle(Arg.Any<MockUseCaseRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void TestNotificationListener_Is_Triggered()
        {
            _notificationListener1 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener2 = Substitute.For<INotificationListener<MockNotification>>();

            _handlerResolver.ResolveListeners<MockNotification>()
                .Returns(new List<INotificationListener<MockNotification>>() {_notificationListener1, _notificationListener2});

            _useCaseMediator.Send(new MockNotification());

            _notificationListener1.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
            _notificationListener2.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }
    }
}