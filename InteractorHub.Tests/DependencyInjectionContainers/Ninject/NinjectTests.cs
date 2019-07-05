using InteractorHub.Tests.Mocks;

namespace InteractorHub.Tests.DependencyInjectionContainers.Ninject
{
    [TestFixture]
    public class NinjectTests
    {

        private IKernel _kernel;
        private IUseCaseMediator _mediator;
        private IUseCaseInteractor<MockUseCaseRequest, MockResponse> _useCaseInteractor;
        private INotificationListener<MockNotification> _notificationListener1;
        private INotificationListener<MockNotification> _notificationListener2;
        private INotificationListener<MockNotification> _notificationListener3;
        [SetUp]
        public void Setup()
        {

            _kernel = new StandardKernel();
            _useCaseInteractor = Substitute.For<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
            _notificationListener1 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener2 = Substitute.For<INotificationListener<MockNotification>>();
            _notificationListener3 = Substitute.For<INotificationListener<MockNotification>>();

            _kernel.Bind<INotificationListener<MockNotification>>().ToMethod(context => _notificationListener1);
            _kernel.Bind<INotificationListener<MockNotification>>().ToMethod(context => _notificationListener1);
            _kernel.Bind<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>()
                .ToMethod(context => _useCaseInteractor);

            _mediator = new UseCaseMediator(new ByDelegateResolver(x => _kernel.Get(x)));
        }

        [Test]
        public async Task Test_Ninject_Resolver()
        {
            await _mediator.Handle<MockResponse, MockUseCaseRequest>(new MockUseCaseRequest());
            await _useCaseInteractor.Received().Handle(Arg.Any<MockUseCaseRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_Ninject_NotificationListeners()
        {
            await _mediator.Send(new MockNotification());
            await _notificationListener1.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
            await _notificationListener2.Received().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Test_Ninject_NotificationListener_NotCalled()
        {
            await _mediator.Send(new MockNotification());
            await _notificationListener3.DidNotReceive().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        }
    }
}
