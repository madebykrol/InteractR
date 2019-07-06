using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractorHub.Interactor;
using InteractorHub.Notification;
using InteractorHub.Resolver;
using InteractorHub.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

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
            var interactor = Substitute.For<IInteractor<MockInteractionRequest, MockResponse>>();
            _resolver.Register(interactor);
        }

        [Test]
        public void CanResolve_Interactor_ByInterface()
        {
            var interactor = new MockInteractor();
            _resolver.Register(interactor);
            Assert.DoesNotThrow(() =>
            {
                var resolvedInteractor = _resolver.ResolveInteractor<IInteractor<MockInteractionRequest, MockResponse>>();
                resolvedInteractor.Handle(new MockInteractionRequest(), CancellationToken.None);
            });
        }

        [Test]
        public void CanCast_ResolvedInteractor_ToConcreteType()
        {
            var interactor = new MockInteractor();
            _resolver.Register(interactor);
            var resolvedInteractor = _resolver.ResolveInteractor<IInteractor<MockInteractionRequest, MockResponse>>();

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

    public class MockInteractor : IInteractor<MockInteractionRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockInteractionRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse();
        }
    }

}
