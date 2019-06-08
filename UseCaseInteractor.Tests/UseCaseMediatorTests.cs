using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UseCaseMediator.Interactor;
using UseCaseMediator.Notification;
using UseCaseMediator.Resolver;
using UseCaseMediator.Tests.Mocks;

namespace UseCaseMediator.Tests
{
    [TestFixture]
    public class UseCaseMediatorTests
    {
        private IUseCaseMediator _useCaseMediator;
        private IResolver _handlerResolver;
        private IUseCaseInteractor<MockUseCaseRequest, MockResponse> _mockUseCaseInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = Substitute.For<IResolver>();
            _useCaseMediator = new UseCaseMediator(_handlerResolver);
        }

        [Test]
        public void TestQueryDispatcher()
        {
            _mockUseCaseInteractor = Substitute.For<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
            _handlerResolver.ResolveInteractor<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>().Returns(_mockUseCaseInteractor);

            _useCaseMediator.Handle<MockResponse, MockUseCaseRequest>(new MockUseCaseRequest());
            _mockUseCaseInteractor.Received().Handle(Arg.Any<MockUseCaseRequest>(), Arg.Any<CancellationToken>());
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