using System;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests;

[TestFixture]
public class HubTests
{
    private IInteractorHub _interactorHub;
    private IResolver _handlerResolver;
    private IRegistrator _handlerRegistrator;
    private IInteractor<MockUseCase, IMockOutputPort> _mockInteractor;

    [SetUp]
    public void Setup()
    {
        _handlerResolver = new SelfContainedResolver();
        _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
        _handlerRegistrator = _handlerResolver as IRegistrator;

        _interactorHub = new Hub(_handlerResolver);
    }

    [Test]
    public void Interactor_Executes()
    {
        _handlerRegistrator.Register(_mockInteractor);
        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());
        _mockInteractor.Received().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void PipeLineTest_FirstMiddleWare_Executes()
    {
        _handlerRegistrator.Register(_mockInteractor);
        var middleware1 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

        middleware1.Execute(
                Arg.Any<MockUseCase>(), 
                Arg.Any<IMockOutputPort>(),
                d => Task.FromResult(new UseCaseResult(true)),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x => x.Arg<Func<MockUseCase, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>()));

        _handlerRegistrator.Register(middleware1);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        middleware1.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Second_MiddleWare_Executes()
    {
        _handlerRegistrator.Register(_mockInteractor);

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

        _handlerRegistrator.Register(middleware1);
        _handlerRegistrator.Register(middleware2);


        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        middleware2.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Interactor_Executes_AfterLast_Middleware()
    {
        _handlerRegistrator.Register(_mockInteractor);

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

        _handlerRegistrator.Register(middleware1);
        _handlerRegistrator.Register(middleware2);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        middleware1.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());

        middleware2.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());

        _mockInteractor.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void Global_Middleware_Executes()
    {
        _handlerRegistrator.Register(_mockInteractor);

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

        _handlerRegistrator.Register(globalMiddleware);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        globalMiddleware.Received().Execute(
            Arg.Any<MockUseCase>(), 
            Arg.Any<Func<MockUseCase, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Global_Middleware_Terminates()
    {
        _handlerRegistrator.Register(_mockInteractor);

        var globalMiddleware = Substitute.For<IMiddleware>();
        globalMiddleware.Execute(
                Arg.Any<MockUseCase>(),
                d => Task.FromResult(new UseCaseResult(true)),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x =>
            {
                   
            });
        _handlerRegistrator.Register(globalMiddleware);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        _mockInteractor.DidNotReceive().Execute(
            Arg.Any<MockUseCase>(),
            Arg.Any<MockOutputPort>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Interactor_Executes_WithoutPipeline()
    {
        _handlerRegistrator.Register(_mockInteractor);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        _mockInteractor.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(),
            Arg.Any<CancellationToken>());
    }
}