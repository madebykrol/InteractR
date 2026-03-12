using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using InteractR.Notifications;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InteractR.Notifications.RabbitMQ;

public sealed class RabbitMqNotificationInlet : INotificationInlet, IAsyncDisposable
{
    private readonly RabbitMqNotificationOptions _configuration;
    private readonly ILogger<RabbitMqNotificationInlet> _logger;
    private readonly INotificationTypeRegistry _notificationTypeRegistry;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isOpen;

    public RabbitMqNotificationInlet(
        RabbitMqNotificationOptions configuration,
        ILogger<RabbitMqNotificationInlet> logger,
        INotificationTypeRegistry notificationTypeRegistry)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationTypeRegistry = notificationTypeRegistry ?? throw new ArgumentNullException(nameof(notificationTypeRegistry));
    }

    [SuppressMessage("Maintainability", "CA1502", Justification = "RabbitMQ setup requires queue declaration, exchange binding, and consumer registration per subscribed notification type.")]
    public async Task Open(
        Func<NotificationEnvelope, Task<EInletResponse>> envelopeCallback,
        CancellationToken cancellationToken = default,
        EProcessingStrategy strategy = EProcessingStrategy.Sequential)
    {
        ArgumentNullException.ThrowIfNull(envelopeCallback);

        if (_isOpen)
        {
            _logger.LogWarning("RabbitMQ inlet is already open");
            return;
        }

        var subscribedNotificationTypes = _notificationTypeRegistry.NotificationSubscriptionTypes();
        if (subscribedNotificationTypes.Count == 0)
        {
            _logger.LogWarning("No notification subscription types were registered, RabbitMQ inlet will not be opened");
            return;
        }

        var factory = CreateConnectionFactory();
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _configuration.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        foreach (var notificationType in subscribedNotificationTypes)
        {
            var address = _notificationTypeRegistry.ResolveAddress(notificationType);
            var queueName = CreateQueueName(address);
            var exchangeName = address.Subject;
            var routingKey = CreateRouteKeyFromTopic(address.Topic);

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: _configuration.Durable,
                exclusive: _configuration.Exclusive,
                autoDelete: _configuration.AutoDelete,
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: _configuration.ExchangeType,
                durable: _configuration.Durable,
                autoDelete: _configuration.AutoDelete,
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey,
                arguments: null,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                if (strategy == EProcessingStrategy.Parallel)
                {
                    _ = Task.Run(() => HandleMessageAsync(eventArgs, envelopeCallback, CancellationToken.None), CancellationToken.None);
                    return;
                }

                await HandleMessageAsync(eventArgs, envelopeCallback, CancellationToken.None);
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: _configuration.AutoAck,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Registered RabbitMQ subscription: Queue={Queue}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                queueName,
                exchangeName,
                routingKey);
        }

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => _ = DisposeAsync().AsTask());
        }

        _isOpen = true;
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs eventArgs,
        Func<NotificationEnvelope, Task<EInletResponse>> envelopeCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = TranslateToEnvelope(eventArgs);
            var response = await envelopeCallback(envelope);
            await HandleAcknowledgment(eventArgs, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle RabbitMQ message: Exchange={Exchange}, RoutingKey={RoutingKey}", eventArgs.Exchange, eventArgs.RoutingKey);
            await HandleRejection(eventArgs, cancellationToken);
        }
    }

    private async Task HandleAcknowledgment(
        BasicDeliverEventArgs eventArgs,
        EInletResponse response,
        CancellationToken cancellationToken)
    {
        if (_configuration.AutoAck || _channel is null)
        {
            return;
        }

        if (response == EInletResponse.Ack)
        {
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);
            return;
        }

        await _channel.BasicNackAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            requeue: _configuration.RequeueOnError,
            cancellationToken);
    }

    private async Task HandleRejection(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (_configuration.AutoAck || _channel is null)
        {
            return;
        }

        await _channel.BasicNackAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            requeue: _configuration.RequeueOnError,
            cancellationToken);
    }

    private NotificationEnvelope TranslateToEnvelope(BasicDeliverEventArgs eventArgs)
    {
        var headers = new Dictionary<string, string>();
        if (eventArgs.BasicProperties.Headers != null)
        {
            foreach (var header in eventArgs.BasicProperties.Headers)
            {
                headers[header.Key] = header.Value switch
                {
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
                    null => string.Empty,
                    _ => header.Value.ToString() ?? string.Empty
                };
            }
        }

        headers["route-key"] = eventArgs.RoutingKey;

        return new NotificationEnvelope
        {
            MessageId = eventArgs.BasicProperties.MessageId ?? Guid.NewGuid().ToString(),
            CausalityId = eventArgs.BasicProperties.CorrelationId,
            Subject = eventArgs.Exchange,
            Topic = eventArgs.RoutingKey,
            Headers = headers,
            Payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray())
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        _isOpen = false;
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _configuration.HostName,
            Port = _configuration.Port,
            UserName = _configuration.UserName,
            Password = _configuration.Password,
            VirtualHost = _configuration.VirtualHost ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }

    private string CreateQueueName(NotificationAddress address)
    {
        var clientName = string.IsNullOrWhiteSpace(_configuration.ClientName)
            ? "interactr"
            : _configuration.ClientName;

        var subject = NormalizeSegment(address.Subject);
        var topic = CreateRouteKeyFromTopic(address.Topic);

        return string.Join('.', new[] { clientName, subject, topic }.Where(segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static string NormalizeSegment(string value)
    {
        return Regex.Replace(value ?? string.Empty, @"[^A-Za-z0-9]+", ".", RegexOptions.Compiled)
            .Trim('.')
            .ToLowerInvariant();
    }

    private static string CreateRouteKeyFromTopic(string topic)
    {
        var words = Regex.Split(topic ?? string.Empty, @"(?=\p{Lu})", RegexOptions.Compiled)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Select(word => word.ToLowerInvariant());

        return string.Join('.', words);
    }

}




