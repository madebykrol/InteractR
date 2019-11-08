using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractorHub.Interactor;
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
    }

    public class MockInteractor : IInteractor<MockInteractionRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockInteractionRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse();
        }
    }

}
