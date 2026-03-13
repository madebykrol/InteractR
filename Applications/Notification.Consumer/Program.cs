using InteractR;
using InteractR.Notifications;
using InteractR.Notifications.RabbitMQ;
using InteractR.Resolver;
using Microsoft.Extensions.Logging;

namespace Notification.Consumer;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());

        var resolver = new SelfContainedResolver();
        resolver.Register<OrderPlacedNotification>(new OrderPlacedHandler(loggerFactory.CreateLogger<OrderPlacedHandler>()));

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

        await hub.OpenNotificationInlets();

        Console.WriteLine("Subscribed to OrderPlaced. Press Ctrl+C to exit.");

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            completion.TrySetResult();
        };

        await completion.Task;
    }
}

internal sealed class OrderPlacedNotification
{
    public required string OrderId { get; init; }
    public required DateTime PlacedAtUtc { get; init; }
}

internal sealed class OrderPlacedHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly ILogger<OrderPlacedHandler> _logger;

    public OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    {
        _logger = logger;
    }

    public Task<ENotificationResponse> Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received OrderPlaced notification. OrderId={OrderId}, PlacedAtUtc={PlacedAtUtc}",
            notification.OrderId,
            notification.PlacedAtUtc);

        return Task.FromResult(ENotificationResponse.Completed);
    }
}
