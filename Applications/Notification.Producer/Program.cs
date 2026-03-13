using InteractR;
using InteractR.Notifications;
using InteractR.Notifications.RabbitMQ;
using InteractR.Resolver;
using Microsoft.Extensions.Logging;

namespace Notification.Producer;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());

        var resolver = new SelfContainedResolver();

        await using var outlet = new RabbitMqNotificationOutlet(
            new RabbitMqNotificationOptions
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "backend",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "backend",
                ClientName = "notification-producer",
                Durable = true
            },
            loggerFactory.CreateLogger<RabbitMqNotificationOutlet>());

        resolver.Register((INotificationOutlet)outlet);

        await using var inlet = new RabbitMqNotificationInlet(
            new RabbitMqNotificationOptions
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "backend",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "backend",
                ClientName = "notification-consumer",
                Durable = true
            },
            loggerFactory.CreateLogger<RabbitMqNotificationInlet>(),
            resolver.NotificationTypeRegistry);

        resolver.Register((INotificationInlet)inlet);

        var hub = new Hub(
            resolver,
            resolver.NotificationTypeRegistry,
            new HubOptions(),
            loggerFactory.CreateLogger<Hub>());

        await hub.OpenNotificationOutlets();
        await hub.OpenNotificationInlets();

        var orderId = args.Length > 0 ? args[0] : Guid.NewGuid().ToString("N");
        var notification = new OrderPlacedNotification
        {
            OrderId = orderId,
            PlacedAtUtc = DateTime.UtcNow
        };

        Console.ReadKey();

        await hub.Publish(notification);

        Console.WriteLine($"Published OrderPlaced notification for OrderId={orderId}");

        Console.ReadKey();
    }
}

internal sealed class OrderPlacedNotification
{
    public required string OrderId { get; init; }
    public required DateTime PlacedAtUtc { get; init; }
}
