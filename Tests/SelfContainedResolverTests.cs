using System.Threading;
using System.Threading.Tasks;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests
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
            _resolver.Register(new MockInteractor());
        }

        [Test]
        public void CanResolve_Interactor_ByInterface()
        {
            var interactor = new MockInteractor();
            _resolver.Register(interactor);
            Assert.DoesNotThrow(() =>
            {
                var resolvedInteractor = _resolver.ResolveInteractor<MockUseCase, IMockOutputPort>(new MockUseCase());
                resolvedInteractor.Execute(new MockUseCase(), new MockOutputPort(), CancellationToken.None);
            });
        }

    }

}
