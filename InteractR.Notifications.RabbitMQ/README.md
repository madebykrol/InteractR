# InteractR.Notifications.RabbitMQ

RabbitMQ transport for `InteractR` notifications.

This package provides:
- `RabbitMqNotificationOutlet` for publishing `NotificationEnvelope` messages to RabbitMQ
- `RabbitMqNotificationInlet` for consuming RabbitMQ messages and forwarding them into the `Hub`
- convention-based exchange, routing key, and queue naming based on `NotificationTypeRegistry`

## Routing model

The transport follows InteractR's `NotificationAddress` model:
- `Subject` -> RabbitMQ exchange
- `Topic` -> RabbitMQ routing key

The outlet publishes using the envelope:
- exchange = `Subject`
- routing key = `Topic`, converted from `PascalCase` to dotted lowercase

Examples:
- `OrderCreatedEvent` -> `Subject = "Order"`, `Topic = "Created"`
  - exchange: `Order`
  - routing key: `created`
- `UserPasswordResetNotification` -> `Subject = "UserPassword"`, `Topic = "Reset"`
  - exchange: `UserPassword`
  - routing key: `reset`
- `[NotificationRoute("Identity", "UserRegistered")]`
  - exchange: `Identity`
  - routing key: `user.registered`

## Inlet subscription model

The inlet is convention-driven.

It does **not** take explicit RabbitMQ subscription configuration per message type.
Instead, it reads registered notification types from `INotificationTypeRegistry` and creates one queue/binding per discovered notification address.

For each registered notification type, the inlet:
1. resolves its `NotificationAddress`
2. creates a queue name from `ClientName + Subject + Topic`
3. declares the exchange named by `Subject`
4. binds the queue using the routing key derived from `Topic`

Queue names are normalized to lowercase dotted segments.

Example:
- `ClientName = "billing-service"`
- notification address: `Subject = "Order"`, `Topic = "Created"`
- queue name: `billing-service.order.created`
- exchange: `Order`
- routing key: `created`

## Options

`RabbitMqNotificationOptions` controls the broker connection and queue behavior:

- `HostName`
- `Port`
- `UserName`
- `Password`
- `VirtualHost`
- `ClientName`
- `ExchangeType`
- `Durable`
- `Exclusive`
- `AutoDelete`
- `AutoAck`
- `RequeueOnError`
- `PrefetchCount`

Example:

```csharp
var options = new RabbitMqNotificationOptions
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    ClientName = "billing-service",
    Durable = true,
    AutoDelete = false,
    PrefetchCount = 10
};
```

## Publishing with the outlet

```csharp
using InteractR.Notifications;
using InteractR.Notifications.RabbitMQ;
using Microsoft.Extensions.Logging.Abstractions;

var outlet = new RabbitMqNotificationOutlet(
    options,
    NullLogger<RabbitMqNotificationOutlet>.Instance);

await outlet.Open();

await outlet.Publish(new NotificationEnvelope
{
    MessageId = Guid.NewGuid().ToString("N"),
    Subject = "Order",
    Topic = "Created",
    Payload = "{\"orderId\":\"123\"}",
    Headers = new Dictionary<string, string>
    {
        ["tenant"] = "backend"
    }
}, CancellationToken.None);
```

## Consuming with the inlet

The inlet requires an `INotificationTypeRegistry` populated with the notification types you want to subscribe to.

```csharp
using InteractR.Notifications;
using InteractR.Notifications.RabbitMQ;
using Microsoft.Extensions.Logging.Abstractions;

var typeRegistry = new NotificationTypeRegistry();
typeRegistry.Register<OrderCreatedEvent>();
typeRegistry.Register<UserRegisteredIntegrationEvent>();

var inlet = new RabbitMqNotificationInlet(
    options,
    NullLogger<RabbitMqNotificationInlet>.Instance,
    typeRegistry);

await inlet.Open(async envelope =>
{
    Console.WriteLine($"Received {envelope.Subject}/{envelope.Topic}");
    return EInletResponse.Ack;
});
```

## Using with `Hub`

In a normal InteractR setup, the registry is usually populated as notification handlers are registered.
For example, `SelfContainedResolver` registers notification types into its `NotificationTypeRegistry` when handlers are added.

```csharp
var resolver = new SelfContainedResolver();
resolver.Register(new OrderCreatedHandler());
resolver.Register(new UserRegisteredIntegrationHandler());

resolver.Register(new RabbitMqNotificationOutlet(
    options,
    NullLogger<RabbitMqNotificationOutlet>.Instance));

resolver.Register(new RabbitMqNotificationInlet(
    options,
    NullLogger<RabbitMqNotificationInlet>.Instance,
    resolver.NotificationTypeRegistry));

var hub = new Hub(resolver, resolver.NotificationTypeRegistry, new HubOptions());
```

## Acknowledgment behavior

The inlet maps `EInletResponse` to broker acknowledgments:
- `EInletResponse.Ack` -> `BasicAck`
- `EInletResponse.Nack` -> `BasicNack`

If `AutoAck` is enabled, RabbitMQ acknowledgments are handled automatically by the broker client.
If `RequeueOnError` is enabled, `Nack` responses and processing failures are requeued.

## Notes

- The transport is built around InteractR address conventions, not explicit per-type broker configuration.
- The inlet reconstructs `NotificationEnvelope.Subject` and `NotificationEnvelope.Topic` from RabbitMQ exchange and routing key data.
- For tests or lightweight setups, `Microsoft.Extensions.Logging.Abstractions` with `NullLogger<T>.Instance` works well.
