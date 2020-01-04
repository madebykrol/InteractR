using System;
using System.Collections.Generic;
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
    public class HubTests
    {
        private IInteractorHub _interactorHub;
        private IResolver _handlerResolver;
        private IInteractor<MockUseCase, IMockOutputPort> _mockInteractor;

        [SetUp]
        public void Setup()
        {
            _handlerResolver = Substitute.For<IResolver>();
            _interactorHub = new Hub(_handlerResolver);
        }

        [Test]
        public void Interactor_Executes()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());
            _mockInteractor.Received().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void PipeLineTest_FirstMiddleWare_Executes()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            var middleware1 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

            middleware1.Execute(
                Arg.Any<MockUseCase>(), 
                Arg.Any<IMockOutputPort>(),
                d => Task.FromResult(new UseCaseResult(true)),
                Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(x => new UseCaseResult(true))
                .AndDoes(x => x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>()));

            IReadOnlyList<IMiddleware<MockUseCase, IMockOutputPort>> pipeline = new List<IMiddleware<MockUseCase, IMockOutputPort>> { middleware1};

            _handlerResolver.ResolveMiddleware<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(pipeline);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());

            middleware1.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void Second_MiddleWare_Executes()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            var middleware1 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

            middleware1.Execute(
                Arg.Any<MockUseCase>(),
                Arg.Any<IMockOutputPort>(),
                d => Task.FromResult( new UseCaseResult(true)), 
                Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs( x => new UseCaseResult(true) )
                .AndDoes(x => x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>()));

            var middleware2 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

            middleware2.Execute(
                Arg.Any<MockUseCase>(),
                Arg.Any<IMockOutputPort>(),
                d => Task.FromResult(new UseCaseResult(true)),
                Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(x => new UseCaseResult(true))
                .AndDoes(x => x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>()));

            IReadOnlyList<IMiddleware<MockUseCase, IMockOutputPort>> pipeline = new List<IMiddleware<MockUseCase, IMockOutputPort>> {middleware1, middleware2};

           

            _handlerResolver.ResolveMiddleware<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(pipeline);


            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());

            middleware2.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void Interactor_Executes_AfterLast_Middleware()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            var middleware1 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

            middleware1.Execute(
                    Arg.Any<MockUseCase>(),
                    Arg.Any<IMockOutputPort>(),
                    d => Task.FromResult(new UseCaseResult(true)),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(x => new UseCaseResult(true))
                .AndDoes(x => x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>()));

            var middleware2 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

            middleware2.Execute(
                    Arg.Any<MockUseCase>(),
                    Arg.Any<IMockOutputPort>(),
                    d => Task.FromResult(new UseCaseResult(true)),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(x => new UseCaseResult(true))
                .AndDoes(x => x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>()));

            IReadOnlyList<IMiddleware<MockUseCase, IMockOutputPort>> pipeline = new List<IMiddleware<MockUseCase, IMockOutputPort>> { middleware1, middleware2 };

            _handlerResolver.ResolveMiddleware<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(pipeline);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());

            middleware2.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void Global_Middleware_Executes()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            var globalMiddleware = Substitute.For<IMiddleware>();
            globalMiddleware.Execute(
                    Arg.Any<MockUseCase>(),
                    d => Task.FromResult(new UseCaseResult(true)),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(x => new UseCaseResult(true))
                .AndDoes(x =>
                {
                    x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>());
                });
            IReadOnlyList<IMiddleware> globalPipeline = new List<IMiddleware> { globalMiddleware };
            _handlerResolver.ResolveGlobalMiddleware().Returns(globalPipeline);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());

            globalMiddleware.Received().Execute(
                Arg.Any<MockUseCase>(), 
                Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void Global_Middleware_Terminates()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);

            var globalMiddleware = Substitute.For<IMiddleware>();
            globalMiddleware.Execute(
                    Arg.Any<MockUseCase>(),
                    d => Task.FromResult(new UseCaseResult(true)),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(x => new UseCaseResult(true))
                .AndDoes(x =>
                {
                   
                });
            IReadOnlyList<IMiddleware> globalPipeline = new List<IMiddleware> { globalMiddleware };
            _handlerResolver.ResolveGlobalMiddleware().Returns(globalPipeline);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());

            _mockInteractor.DidNotReceive().Execute(
                Arg.Any<MockUseCase>(),
                Arg.Any<MockOutputPort>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void Interactor_Executes_WithoutPipeline()
        {
            _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
            _handlerResolver.ResolveInteractor<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(_mockInteractor);


            IReadOnlyList<IMiddleware<MockUseCase, IMockOutputPort>> pipeline = new List<IMiddleware<MockUseCase, IMockOutputPort>> { };

            _handlerResolver.ResolveMiddleware<MockUseCase, IMockOutputPort>(Arg.Any<MockUseCase>()).Returns(pipeline);

            _interactorHub.Execute(new MockUseCase(), (IMockOutputPort)new MockOutputPort());

            _mockInteractor.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(),
                Arg.Any<CancellationToken>());
        }
    }
}