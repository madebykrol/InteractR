using InteractorHub.Tests.Mocks;

namespace InteractorHub.Tests.DependencyInjectionContainers
{
    [TestFixture]
    public class AutoFacTests
    {
        private IContainer _container;
        private IUseCaseMediator _mediator;
        private IUseCaseInteractor<MockUseCaseRequest, MockResponse> _useCaseInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;
        private INotificationListener<MockNotification> _notificationListener3;

        [SetUp]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            _useCaseInteractor = Substitute.For<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
            _notificationListener1 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener2 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener3 = Substitute.For<INotificationListener<MockNotification>>();

            builder.RegisterInstance(_useCaseInteractor).As<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
            builder.RegisterInstance(_notificationListener1).As<INotificationListener<MockNotification>>();
            builder.RegisterInstance(_notificationListener2).As<INotificationListener<MockNotification>>();

            _container = builder.Build();

            _mediator = new UseCaseMediator(new ByDelegateResolver(_container.Resolve));
        }

        [Test]
        public async Task Test_AutoFac_Resolver()
        {
            await _mediator.Handle<MockResponse, MockUseCaseRequest>(new MockUseCaseRequest());
            await _useCaseInteractor.Received().Handle(Arg.Any<MockUseCaseRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_AutoFac_NotificationListeners()
        {
            await _mediator.Send(new MockNotification());
            await _notificationListener1.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
            await _notificationListener2.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_AutoFac_NotificationListener_NotCalled()
        {
            await _mediator.Send(new MockNotification());
            await _notificationListener3.DidNotReceive().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }
    }
}
