using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;
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
        public void Can_Register_Middleware()
        {
            _resolver.Register(Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>());
            var middleware = _resolver.ResolveMiddleware<MockUseCase, IMockOutputPort>(new MockUseCase());

            Assert.That(middleware != null);
        }

        [Test]
        public void Can_Resolve_Middleware()
        {
            var middleware = Substitute.For<IMiddleware<IHasPolicy>>();
            _resolver.Register(middleware);

            _resolver.ResolveMiddleware<MockUseCase>().FirstOrDefault()
                .Execute(new MockUseCase(), null, CancellationToken.None);

            middleware.Received(1).Execute(Arg.Any<MockUseCase>(),
                    Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(), Arg.Any<CancellationToken>());
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
