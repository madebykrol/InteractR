using System;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using InteractR.Notifications;

namespace InteractR.Tests;

[TestFixture]
public class HubTests
{
    private IHub _interactorHub;
    private Hub _hub;
    private IResolver _handlerResolver;
    private IRegistrator _handlerRegistrator;
    private IInteractor<MockUseCase, IMockOutputPort> _mockInteractor;

    [SetUp]
    public void Setup()
    {
        var resolver = new SelfContainedResolver();
        _handlerResolver = resolver;
        _mockInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
        _handlerRegistrator = resolver;

        _hub = new Hub(resolver, resolver.NotificationTypeRegistry, new HubOptions());
        _interactorHub = _hub;
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
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x => x.Arg<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>(), null));

        _handlerRegistrator.Register(middleware1);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        middleware1.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
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
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs( x => new UseCaseResult(true) )
            .AndDoes(x => x.Arg<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>(), null));

        var middleware2 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

        middleware2.Execute(
                Arg.Any<MockUseCase>(),
                Arg.Any<IMockOutputPort>(),
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x => x.Arg<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>(), null));

        _handlerRegistrator.Register(middleware1);
        _handlerRegistrator.Register(middleware2);


        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        middleware2.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
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
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x => x.Arg<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>(), null));

        var middleware2 = Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>();

        middleware2.Execute(
                Arg.Any<MockUseCase>(),
                Arg.Any<IMockOutputPort>(),
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x => x.Arg<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>(), null));

        _handlerRegistrator.Register(middleware1);
        _handlerRegistrator.Register(middleware2);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        middleware1.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());

        middleware2.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
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
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x => new UseCaseResult(true))
            .AndDoes(x =>
            {
                x.Arg<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>().Invoke(x.Arg<MockUseCase>(), null);
            });

        _handlerRegistrator.Register(globalMiddleware);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        globalMiddleware.Received().Execute(
            Arg.Any<MockUseCase>(), 
            Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Global_Middleware_Terminates()
    {
        _handlerRegistrator.Register(_mockInteractor);

        var globalMiddleware = Substitute.For<IMiddleware>();
        globalMiddleware.Execute(
                Arg.Any<MockUseCase>(),
                Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(),
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

    [Test]
    public void Run_Executes_Interactor()
    {
        _handlerRegistrator.Register(_mockInteractor);

        _hub.Run(new MockUseCase(), new MockOutputPort());

        _mockInteractor.ReceivedWithAnyArgs().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Run_Executes_Interactor_WithCancellationToken()
    {
        _handlerRegistrator.Register(_mockInteractor);
        var cancellationToken = new CancellationTokenSource().Token;

        _hub.Run(new MockUseCase(), new MockOutputPort(), cancellationToken);

        _mockInteractor.Received().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), cancellationToken);
    }

    [Test]
    public async Task Publish_Executes_NotificationHandlers()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        _handlerRegistrator.Register(handler);

        await _interactorHub.Publish(new MockNotification());

        await handler.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_Dispatches_To_NotificationOutlet_ForInProcessNotifications()
    {
        var outlet = Substitute.For<INotificationOutlet>();
        _handlerRegistrator.Register(outlet);

        await _interactorHub.Publish(new MockNotification());

        await outlet.Received(1).Publish(Arg.Any<NotificationEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_Processes_BrokerEcho_WhenSameMessageIsReceivedFromInlet()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        var outlet = Substitute.For<INotificationOutlet>();
        _handlerRegistrator.Register(handler);
        _handlerRegistrator.Register(outlet);

        // Publish in-process with known messageId
        await _hub.Publish(new MockNotification(), "message-1");

        // Simulate broker echo via inlet with same messageId
        _handlerRegistrator.Register(new MockNotificationInlet(new MockNotification(), "message-1"));
        await _hub.OpenNotificationInlets();

        // Handler called for both the in-process publish and the inlet message
        await handler.Received(2).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        // Outlet called once (in-process publish only)
        await outlet.Received(1).Publish(Arg.Any<NotificationEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_Uses_Provided_MessageId_For_NotificationEnvelope()
    {
        var outlet = Substitute.For<INotificationOutlet>();
        _handlerRegistrator.Register(outlet);

        await _hub.Publish(new MockNotification(), "message-1");

        await outlet.Received(1).Publish(
            Arg.Is<NotificationEnvelope>(x => x.MessageId == "message-1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartNotificationInlets_InvokesInProcessHandlers_WithoutRepublishingToOutlets()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        var outlet = Substitute.For<INotificationOutlet>();
        _handlerRegistrator.Register(new MockNotificationInlet(new MockNotification(), "inlet-message"));
        _handlerRegistrator.Register(handler);
        _handlerRegistrator.Register(outlet);

        await _interactorHub.OpenNotificationInlets();

        await handler.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await outlet.DidNotReceive().Publish(Arg.Any<NotificationEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_Processes_InletNotification_Each_Time_It_Is_Received()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        _handlerRegistrator.Register(handler);

        var messageId = "test-message-1";

        // Publish in-process with known messageId
        await _hub.Publish(new MockNotification(), messageId);

        // Simulate out-of-process duplicate via inlet
        _handlerRegistrator.Register(new MockNotificationInlet(new MockNotification(), messageId));
        await _hub.OpenNotificationInlets();

        // Handler should be called for both the in-process publish and the inlet message
        await handler.Received(2).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }


    [Test]
    public void Middleware_Executes_In_Order_WhenOrderedMiddlewareIsUsed()
    {
        _handlerRegistrator.Register(_mockInteractor);
        var executionOrder = new List<string>();

        _mockInteractor.Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new UseCaseResult(true)))
            .AndDoes(_ => executionOrder.Add("interactor"));

        _handlerRegistrator.Register(new OrderedMockMiddleware(2, "second", executionOrder));
        _handlerRegistrator.Register(new OrderedMockMiddleware(1, "first", executionOrder));

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        Assert.That(executionOrder, Is.EqualTo(new[] { "first", "second", "interactor" }));
    }

    [Test]
    public void Middleware_Is_Skipped_WhenConditionalMiddlewareReturnsFalse()
    {
        _handlerRegistrator.Register(_mockInteractor);
        var conditionalMiddleware = new ConditionalMockMiddleware(false);
        _handlerRegistrator.Register(conditionalMiddleware);

        _interactorHub.Execute(new MockUseCase(), new MockOutputPort());

        Assert.That(conditionalMiddleware.Executed, Is.False);
        _mockInteractor.ReceivedWithAnyArgs(1).Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
    }

    private sealed class OrderedMockMiddleware : IMiddleware<MockUseCase, IMockOutputPort>, IOrdered
    {
        private readonly string _name;
        private readonly IList<string> _executionOrder;

        public OrderedMockMiddleware(int order, string name, IList<string> executionOrder)
        {
            Order = order;
            _name = name;
            _executionOrder = executionOrder;
        }

        public int Order { get; }

        public Task<UseCaseResult> Execute(MockUseCase usecase, IMockOutputPort outputPort, Func<MockUseCase, CancellationToken?, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        {
            _executionOrder.Add(_name);
            return next(usecase, null);
        }
    }

    private sealed class ConditionalMockMiddleware : IMiddleware<MockUseCase, IMockOutputPort>, IConditionalMiddleware<MockUseCase>
    {
        private readonly bool _shouldExecute;

        public ConditionalMockMiddleware(bool shouldExecute)
        {
            _shouldExecute = shouldExecute;
        }

        public bool Executed { get; private set; }

        public bool ShouldExecute(MockUseCase usecase) => _shouldExecute;

        public Task<UseCaseResult> Execute(MockUseCase usecase, IMockOutputPort outputPort, Func<MockUseCase, CancellationToken?, Task<UseCaseResult>> next, CancellationToken cancellationToken)
        {
            Executed = true;
            return next(usecase, null);
        }
    }

    private sealed class MockNotificationInlet : INotificationInlet
    {
        private readonly MockNotification _notification;
        private readonly string _messageId;

        public MockNotificationInlet(MockNotification notification, string messageId)
        {
            _notification = notification;
            _messageId = messageId;
        }

        public async Task Open(Func<NotificationEnvelope, Task<EInletResponse>> ingress, CancellationToken cancellationToken = default, EProcessingStrategy strategy = EProcessingStrategy.Sequential)
        {
            var registry = new NotificationTypeRegistry();
            var address = registry.ResolveAddress(typeof(MockNotification));
            var envelope = new NotificationEnvelope
            {
                MessageId = _messageId,
                Subject = address.Subject,
                Topic = address.Topic,
                Payload = System.Text.Json.JsonSerializer.Serialize(_notification),
            };

            await ingress(envelope);
        }
    }
}