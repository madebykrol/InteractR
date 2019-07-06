using System.Threading;
using System.Threading.Tasks;
using InteractorHub.Interactor;
using InteractorHub.Notification;
using InteractorHub.Resolver;
using InteractorHub.Resolvers.Ninject;
using InteractorHub.Tests.Mocks;
using Ninject;
using NSubstitute;
using NUnit.Framework;

namespace InteractorHub.Tests.DependencyInjectionContainers.Ninject
{
    [TestFixture]
    public class NinjectTests
    {

        private IKernel _kernel;
        private IHub _hub;
        private IInteractor<MockInteractionRequest, MockResponse> _useCaseInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;
        private INotificationListener<MockNotification> _notificationListener3;
        [SetUp]
        public void Setup()
        {

            _kernel = new StandardKernel();
            _useCaseInteractor = Substitute.For<IInteractor<MockInteractionRequest, MockResponse>>();
            _notificationListener1 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener2 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener3 = Substitute.For<INotificationListener<MockNotification>>();

            _kernel.Bind<INotificationListener<MockNotification>>().ToMethod(context => _notificationListener1);
            _kernel.Bind<INotificationListener<MockNotification>>().ToMethod(context => _notificationListener2);
            _kernel.Bind<IInteractor<MockInteractionRequest, MockResponse>>()
                .ToMethod(context => _useCaseInteractor);

            _hub = new Hub(new NinjectResolver(_kernel));
        }

        [Test]
        public async Task Test_Ninject_Resolver()
        {
            await _hub.Handle<MockResponse, MockInteractionRequest>(new MockInteractionRequest());
            await _useCaseInteractor.Received().Handle(Arg.Any<MockInteractionRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_Ninject_NotificationListeners()
        {
            await _hub.Send(new MockNotification());
            await _notificationListener1.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
            await _notificationListener2.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_Ninject_NotificationListener_NotCalled()
        {
            await _hub.Send(new MockNotification());
            await _notificationListener3.DidNotReceive().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }
    }
}
