using InteractR;
using InteractR.Notifications;
using InteractR.Notifications.RabbitMQ;
using InteractR.Resolver;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;

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

        var hub = new Hub(
            resolver,
            resolver.NotificationTypeRegistry,
            new HubOptions(),
            loggerFactory.CreateLogger<Hub>());

        await hub.OpenOutlets();

        await Publish(hub, args);

    }


    private static async Task Publish(Hub hub, string[] args)
    {

        var orderId = args.Length > 0 ? args[0] : Guid.NewGuid().ToString("N");
        var notification = new OrderPlacedNotification
        {
            OrderId = orderId,
            PlacedAtUtc = DateTime.UtcNow
        };

        await hub.Publish(notification);

        Console.WriteLine($"Published OrderPlaced notification for OrderId={orderId}");
    }
}

internal sealed class OrderPlacedNotification
{
    public required string OrderId { get; init; }
    public required DateTime PlacedAtUtc { get; init; }
}
