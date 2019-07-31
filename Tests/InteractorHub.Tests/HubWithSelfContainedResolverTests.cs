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
    public class HubWithSelfContainedResolverTests
    {
        private IHub _useCaseMediator;
        private SelfContainedResolver _handlerResolver;
        private IInteractor<MockInteractionRequest, MockResponse> _mockUseCaseInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;

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
            _useCaseMediator.Handle<MockResponse, MockInteractionRequest>(new MockInteractionRequest());
            _mockUseCaseInteractor.Received().Handle(Arg.Any<MockInteractionRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void TestNotificationListener_Is_Triggered()
        {
            _notificationListener1 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener2 = Substitute.For<INotificationListener<MockNotification>>();

            _handlerResolver.Register(_notificationListener1);
            _handlerResolver.Register(_notificationListener2);

            _useCaseMediator.Send(new MockNotification());

            _notificationListener1.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
            _notificationListener2.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }
    }
}