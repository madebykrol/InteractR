using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using NSubstitute;
using NUnit.Framework;
using UseCaseMediator.Interactor;
using UseCaseMediator.Resolver;
using UseCaseMediator.Tests.Mocks;

namespace UseCaseMediator.Tests.DependencyInjectionContainers
{
    [TestFixture]
    public class AutoFacTests
    {
        private IContainer _container;
        private IUseCaseMediator _mediator;

        [SetUp]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new MockUseCaseInteractor()).As<IUseCaseInteractor<MockUseCaseRequest, MockResponse>>();
            _container = builder.Build();

            _mediator = new UseCaseMediator(new ByDelegateResolver(_container.Resolve));
        }

        [Test]
        public async Task Test_AutoFac_Resolver()
        {
            var response = await _mediator.Handle<MockResponse, MockUseCaseRequest>(new MockUseCaseRequest());
            Assert.That(response.HasBeenHandled);
        }
    }
}
