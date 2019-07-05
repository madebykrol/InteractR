using InteractorHub.Tests.Mocks;

namespace InteractorHub.Tests
{
    [TestFixture]
    public class SelfContainedResolverTests
    {
        private SelfContainedResolver _resolver;

        [SetUp]
        public void Setup()
        {
            _resolver = new SelfContainedResolver();
        }

        [Test]
        public void Can_Register_Interactor()
        {
            var interactor = Substitute.For<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
            _resolver.Register(interactor);
        }

        [Test]
        public void CanResolve_Interactor_ByInterface()
        {
            var interactor = new MockInteractor();
            _resolver.Register(interactor);
            Assert.DoesNotThrow(() =>
            {
                var resolvedInteractor = _resolver.ResolveInteractor<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
                resolvedInteractor.Handle(new MockUseCaseRequest(), CancellationToken.None);
            });
        }

        [Test]
        public void CanCast_ResolvedInteractor_ToConcreteType()
        {
            var interactor = new MockInteractor();
            _resolver.Register(interactor);
            var resolvedInteractor = _resolver.ResolveInteractor<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();

            Assert.IsInstanceOf<MockInteractor>(resolvedInteractor);
        }

        [Test]
        public void Can_Register_NotificationListener()
        {
            var listener = Substitute.For<INotificationListener<MockNotification>>();
            _resolver.Register(listener);
        }

        [Test]
        public void Can_Resolve_NotificationListener()
        {
            var listener = Substitute.For<INotificationListener<MockNotification>>();
            _resolver.Register(listener);
            var listeners = _resolver.ResolveListeners<MockNotification>();

            Assert.That(listeners.Any());
        }

        [Test]
        public void Can_Resolve_MultipleNotificationListener()
        {
            var listener = Substitute.For<INotificationListener<MockNotification>>();
            var listener2 = Substitute.For<INotificationListener<MockNotification>>();
            _resolver.Register(listener);
            _resolver.Register(listener2);
            var listeners = _resolver.ResolveListeners<MockNotification>();

            Assert.That(listeners.Count() == 2);
        }
    }

    public class MockInteractor : IUseCaseInteractor<MockUseCaseRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockUseCaseRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse();
        }
    }

}
