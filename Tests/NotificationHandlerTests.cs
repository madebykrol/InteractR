using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Notifications;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests;

[TestFixture]
public class NotificationHandlerTests
{
    private Hub _hub;
    private SelfContainedResolver _resolver;
    private ILogger<Hub> _logger;

    [SetUp]
    public void Setup()
    {
        _resolver = new SelfContainedResolver();
        _logger = Substitute.For<ILogger<Hub>>();
        _hub = new Hub(_resolver, _resolver.NotificationTypeRegistry, new HubOptions(), _logger);
    }

    #region Basic Handler Execution

    [Test]
    public async Task Handler_Receives_Notification()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        _resolver.Register(handler);

        var notification = new MockNotification();
        await _hub.Publish(notification);

        await handler.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handler_Receives_CancellationToken()
    {
        var receivedToken = CancellationToken.None;
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                receivedToken = callInfo.ArgAt<CancellationToken>(1);
                return Task.FromResult(ENotificationResponse.Completed);
            });
        _resolver.Register(handler);

        using var cts = new CancellationTokenSource();
        await _hub.Publish(new MockNotification(), cts.Token);

        Assert.That(receivedToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task Publish_WithoutHandlers_DoesNotThrow()
    {
        // No handlers registered
        Assert.DoesNotThrowAsync(async () => await _hub.Publish(new MockNotification()));
    }

    #endregion

    #region Multiple Handlers

    [Test]
    public async Task Multiple_Handlers_All_Execute()
    {
        var handler1 = Substitute.For<INotificationHandler<MockNotification>>();
        var handler2 = Substitute.For<INotificationHandler<MockNotification>>();
        var handler3 = Substitute.For<INotificationHandler<MockNotification>>();

        handler1.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        handler2.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        handler3.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(handler1);
        _resolver.Register(handler2);
        _resolver.Register(handler3);

        await _hub.Publish(new MockNotification());

        await handler1.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await handler3.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Multiple_Handlers_Execute_Sequentially()
    {
        var executionOrder = new List<string>();
        var handler1 = new TrackingNotificationHandler("handler1", executionOrder);
        var handler2 = new TrackingNotificationHandler("handler2", executionOrder);
        var handler3 = new TrackingNotificationHandler("handler3", executionOrder);

        _resolver.Register<MockNotification>(handler1);
        _resolver.Register<MockNotification>(handler2);
        _resolver.Register<MockNotification>(handler3);

        await _hub.Publish(new MockNotification());

        Assert.That(executionOrder, Is.EqualTo(new[] { "handler1", "handler2", "handler3" }));
    }

    [Test]
    public async Task Handlers_For_Different_NotificationTypes_Are_Independent()
    {
        var mockHandler = Substitute.For<INotificationHandler<MockNotification>>();
        var orderHandler = Substitute.For<INotificationHandler<OrderCreatedNotification>>();

        mockHandler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        orderHandler.Handle(Arg.Any<OrderCreatedNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(mockHandler);
        _resolver.Register(orderHandler);

        await _hub.Publish(new MockNotification());

        await mockHandler.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await orderHandler.DidNotReceive().Handle(Arg.Any<OrderCreatedNotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handler Response Handling

    [Test]
    public async Task Handler_Returning_Completed_Continues_To_Next_Handler()
    {
        var handler1 = Substitute.For<INotificationHandler<MockNotification>>();
        var handler2 = Substitute.For<INotificationHandler<MockNotification>>();

        handler1.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));
        handler2.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(handler1);
        _resolver.Register(handler2);

        await _hub.Publish(new MockNotification());

        await handler1.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handler_Returning_Ignored_Continues_To_Next_Handler()
    {
        var handler1 = Substitute.For<INotificationHandler<MockNotification>>();
        var handler2 = Substitute.For<INotificationHandler<MockNotification>>();

        handler1.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Ignored));
        handler2.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(handler1);
        _resolver.Register(handler2);

        await _hub.Publish(new MockNotification());

        await handler1.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handler_Returning_Failed_Continues_To_Next_Handler()
    {
        var handler1 = Substitute.For<INotificationHandler<MockNotification>>();
        var handler2 = Substitute.For<INotificationHandler<MockNotification>>();

        handler1.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Failed));
        handler2.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(handler1);
        _resolver.Register(handler2);

        await _hub.Publish(new MockNotification());

        await handler1.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Notification Data Preservation

    [Test]
    public async Task Handler_Receives_Same_Notification_Instance()
    {
        MockNotification receivedNotification = null;
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                receivedNotification = callInfo.ArgAt<MockNotification>(0);
                return Task.FromResult(ENotificationResponse.Completed);
            });
        _resolver.Register(handler);

        var notification = new MockNotification();
        await _hub.Publish(notification);

        Assert.That(receivedNotification, Is.SameAs(notification));
    }

    [Test]
    public async Task Handler_Receives_Notification_With_Data()
    {
        OrderCreatedNotification receivedNotification = null;
        var handler = Substitute.For<INotificationHandler<OrderCreatedNotification>>();
        handler.Handle(Arg.Any<OrderCreatedNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                receivedNotification = callInfo.ArgAt<OrderCreatedNotification>(0);
                return Task.FromResult(ENotificationResponse.Completed);
            });
        _resolver.Register(handler);

        var notification = new OrderCreatedNotification
        {
            OrderId = "ORDER-123",
            CustomerId = "CUST-456",
            Amount = 99.99m
        };
        await _hub.Publish(notification);

        Assert.That(receivedNotification.OrderId, Is.EqualTo("ORDER-123"));
        Assert.That(receivedNotification.CustomerId, Is.EqualTo("CUST-456"));
        Assert.That(receivedNotification.Amount, Is.EqualTo(99.99m));
    }

    #endregion

    #region Handler Exception Handling

    [Test]
    public async Task Handler_Throwing_Exception_Propagates_To_Caller()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns<Task<ENotificationResponse>>(x => throw new InvalidOperationException("Handler error"));
        _resolver.Register(handler);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _hub.Publish(new MockNotification()));

        Assert.That(ex.Message, Is.EqualTo("Handler error"));
    }

    [Test]
    public async Task First_Handler_Exception_Prevents_Subsequent_Handlers()
    {
        var handler1 = Substitute.For<INotificationHandler<MockNotification>>();
        var handler2 = Substitute.For<INotificationHandler<MockNotification>>();

        handler1.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns<Task<ENotificationResponse>>(x => throw new InvalidOperationException("First handler failed"));
        handler2.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(handler1);
        _resolver.Register(handler2);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _hub.Publish(new MockNotification()));

        await handler1.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await handler2.DidNotReceive().Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Async Handler Behavior

    [Test]
    public async Task Handler_Async_Operation_Completes_Before_Next_Handler()
    {
        var executionLog = new List<string>();
        var handler1 = new AsyncTrackingHandler("handler1", executionLog, TimeSpan.FromMilliseconds(50));
        var handler2 = new AsyncTrackingHandler("handler2", executionLog, TimeSpan.FromMilliseconds(10));

        _resolver.Register<MockNotification>(handler1);
        _resolver.Register<MockNotification>(handler2);

        await _hub.Publish(new MockNotification());

        // Despite handler2 having shorter delay, handler1 should complete first (sequential)
        Assert.That(executionLog, Is.EqualTo(new[]
        {
            "handler1-start",
            "handler1-end",
            "handler2-start",
            "handler2-end"
        }));
    }

    #endregion

    #region Outlet Integration

    [Test]
    public async Task Notification_Is_Sent_To_Outlets_After_Handlers()
    {
        var handler = Substitute.For<INotificationHandler<MockNotification>>();
        var outlet = Substitute.For<INotificationOutlet>();

        handler.Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ENotificationResponse.Completed));

        _resolver.Register(handler);
        _resolver.Register(outlet);

        await _hub.Publish(new MockNotification());

        await handler.Received(1).Handle(Arg.Any<MockNotification>(), Arg.Any<CancellationToken>());
        await outlet.Received(1).Publish(Arg.Any<NotificationEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Notification_Sent_To_Multiple_Outlets()
    {
        var outlet1 = Substitute.For<INotificationOutlet>();
        var outlet2 = Substitute.For<INotificationOutlet>();

        _resolver.Register(outlet1);
        _resolver.Register(outlet2);

        await _hub.Publish(new MockNotification());

        await outlet1.Received(1).Publish(Arg.Any<NotificationEnvelope>(), Arg.Any<CancellationToken>());
        await outlet2.Received(1).Publish(Arg.Any<NotificationEnvelope>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Types

    private sealed class TrackingNotificationHandler : INotificationHandler<MockNotification>
    {
        private readonly string _name;
        private readonly IList<string> _executionOrder;

        public TrackingNotificationHandler(string name, IList<string> executionOrder)
        {
            _name = name;
            _executionOrder = executionOrder;
        }

        public Task<ENotificationResponse> Handle(MockNotification notification, CancellationToken cancellationToken)
        {
            _executionOrder.Add(_name);
            return Task.FromResult(ENotificationResponse.Completed);
        }
    }

    private sealed class AsyncTrackingHandler : INotificationHandler<MockNotification>
    {
        private readonly string _name;
        private readonly IList<string> _executionLog;
        private readonly TimeSpan _delay;

        public AsyncTrackingHandler(string name, IList<string> executionLog, TimeSpan delay)
        {
            _name = name;
            _executionLog = executionLog;
            _delay = delay;
        }

        public async Task<ENotificationResponse> Handle(MockNotification notification, CancellationToken cancellationToken)
        {
            _executionLog.Add($"{_name}-start");
            await Task.Delay(_delay, cancellationToken);
            _executionLog.Add($"{_name}-end");
            return ENotificationResponse.Completed;
        }
    }

    public sealed class OrderCreatedNotification
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
    }

    #endregion
}
