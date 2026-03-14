using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InteractR.Notifications;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace InteractR.Notifications.RabbitMQ;

public sealed class RabbitMqNotificationOutlet : INotificationOutlet, IAsyncDisposable
{
    private readonly RabbitMqNotificationOptions _configuration;
    private readonly ILogger<RabbitMqNotificationOutlet> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isOpen;

    public RabbitMqNotificationOutlet(
        RabbitMqNotificationOptions configuration,
        ILogger<RabbitMqNotificationOutlet> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Open(CancellationToken cancellationToken = default)
    {
        if (_isOpen)
        {
            _logger.LogWarning("RabbitMQ outlet is already open");
            return;
        }

        var factory = CreateConnectionFactory();
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        _isOpen = true;

        _logger.LogInformation(
            "RabbitMQ outlet opened: Host={Host}, Port={Port}",
            _configuration.HostName,
            _configuration.Port);
    }

    public async Task Publish(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!_isOpen || _channel is null)
        {
            _logger.LogWarning("RabbitMQ outlet is not open, notification not published: {Subject}/{Topic}", envelope.Subject, envelope.Topic);
            return;
        }

        var exchangeName = envelope.Subject;
        var routingKey = CreateRouteKeyFromTopic(envelope.Topic);

        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: _configuration.ExchangeType,
            durable: _configuration.Durable,
            autoDelete: _configuration.AutoDelete,
            arguments: null,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CausalityId,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Persistent = _configuration.Durable,
            Headers = CreateHeaders(envelope.Headers)
        };

        var payloadJson = SerializePayload(envelope.Payload);

        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payloadJson),
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published RabbitMQ notification: Exchange={Exchange}, RoutingKey={RoutingKey}, MessageId={MessageId}",
            exchangeName,
            routingKey,
            envelope.MessageId);
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

    private static Dictionary<string, object?>? CreateHeaders(IDictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0)
        {
            return null;
        }

        var messageHeaders = new Dictionary<string, object?>(headers.Count);
        foreach (var header in headers)
        {
            messageHeaders[header.Key] = Encoding.UTF8.GetBytes(header.Value);
        }

        return messageHeaders;
    }

    private static string SerializePayload(object? payload)
    {
        return payload switch
        {
            null => string.Empty,
            string json => json,
            JsonElement element => element.GetRawText(),
            _ => JsonSerializer.Serialize(payload)
        };
    }

    private static string CreateRouteKeyFromTopic(string topic)
    {
        var words = Regex.Split(topic ?? string.Empty, @"(?=\p{Lu})", RegexOptions.Compiled)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Select(word => word.ToLowerInvariant());

        return string.Join('.', words);
    }
}