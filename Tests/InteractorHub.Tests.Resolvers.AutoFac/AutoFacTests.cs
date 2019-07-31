using System.Threading;
using System.Threading.Tasks;
using Autofac;
using InteractorHub.Interactor;
using InteractorHub.Notification;
using InteractorHub.Resolvers.AutoFac;
using InteractorHub.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractorHub.Tests.Resolvers.AutoFac
{
    [TestFixture]
    public class AutoFacTests
    {
        private IContainer _container;
        private IHub _hub;
        private IInteractor<MockInteractionRequest, MockResponse> _useCaseInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;
        private INotificationListener<MockNotification> _notificationListener3;

        [SetUp]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            _useCaseInteractor = Substitute.For<IInteractor<MockInteractionRequest, MockResponse>>();
            _notificationListener1 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener2 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener3 = Substitute.For<INotificationListener<MockNotification>>();

            builder.RegisterInstance(_useCaseInteractor).As<IInteractor<MockInteractionRequest, MockResponse>>();
            builder.RegisterInstance(_notificationListener1).As<INotificationListener<MockNotification>>();
            builder.RegisterInstance(_notificationListener2).As<INotificationListener<MockNotification>>();

            _container = builder.Build();

            _hub = new Hub(new AutoFacResolver(_container));
        }

        [Test]
        public async Task Test_AutoFac_Resolver()
        {
            await _hub.Handle<MockResponse, MockInteractionRequest>(new MockInteractionRequest());
            await _useCaseInteractor.Received().Handle(Arg.Any<MockInteractionRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_AutoFac_NotificationListeners()
        {
            await _hub.Send(new MockNotification());
            await _notificationListener1.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
            await _notificationListener2.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_AutoFac_NotificationListener_NotCalled()
        {
            await _hub.Send(new MockNotification());
            await _notificationListener3.DidNotReceive().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }
    }
}
