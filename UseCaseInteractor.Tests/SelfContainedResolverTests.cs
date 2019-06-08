using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UseCaseMediator.Interactor;
using UseCaseMediator.Resolver;
using UseCaseMediator.Tests.Mocks;

namespace UseCaseMediator.Tests
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
    }

    public class MockInteractor : IUseCaseInteractor<MockUseCaseRequest, MockResponse>
    {
        public async Task<MockResponse> Handle(MockUseCaseRequest request, CancellationToken cancellationToken)
        {
            return new MockResponse();
        }
    }

}
