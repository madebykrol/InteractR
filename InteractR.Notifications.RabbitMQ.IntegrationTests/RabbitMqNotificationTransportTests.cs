using System.Text;
using System.Text.RegularExpressions;
using InteractR.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace InteractR.Notifications.RabbitMQ.IntegrationTests;

[TestFixture]
public class RabbitMqNotificationTransportTests
{
    private const string HostName = "localhost";
    private const string UserName = "backend";
    private const string Password = "backend";
    private int _port;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _port = await TryResolveRabbitMqPortAsync();
        if (_port == 0)
        {
            Assert.Ignore("RabbitMQ is not reachable on localhost:15672 or localhost:5672 with the configured credentials.");
        }
    }

    [Test]
    public async Task Outlet_Publishes_Envelope_To_RabbitMq()
    {
        var subject = $"it.outlet.{Guid.NewGuid():N}";
        var topic = "Created";
        var routingKey = CreateRouteKeyFromTopic(topic);
        var queueName = $"it-outlet-{Guid.NewGuid():N}";
        var firstEnvelope = new NotificationEnvelope
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CausalityId = Guid.NewGuid().ToString("N"),
            Subject = subject,
            Topic = topic,
            Payload = "{\"value\":42}",
            Headers = new Dictionary<string, string>
            {
                ["tenant"] = "backend"
            }
        };

        var observedEnvelope = new NotificationEnvelope
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CausalityId = Guid.NewGuid().ToString("N"),
            Subject = subject,
            Topic = topic,
            Payload = firstEnvelope.Payload,
            Headers = new Dictionary<string, string>(firstEnvelope.Headers)
        };

        await using var outlet = new RabbitMqNotificationOutlet(
            CreateOptions("outlet-client"),
            NullLogger<RabbitMqNotificationOutlet>.Instance);
        await outlet.Open(CancellationToken.None);
        await outlet.Publish(firstEnvelope, CancellationToken.None);

        await using var topologyConnection = await CreateFactory().CreateConnectionAsync();
        await using var topologyChannel = await topologyConnection.CreateChannelAsync();
        await topologyChannel.ExchangeDeclarePassiveAsync(subject);
        await topologyChannel.QueueDeclareAsync(queueName, false, false, true, null);
        await topologyChannel.QueueBindAsync(queueName, subject, routingKey);

        await outlet.Publish(observedEnvelope, CancellationToken.None);

        var message = await WaitForMessageAsync(() => topologyChannel.BasicGetAsync(queueName, true));

        Assert.Multiple(() =>
        {
            Assert.That(message, Is.Not.Null);
            Assert.That(message!.BasicProperties.MessageId, Is.EqualTo(observedEnvelope.MessageId));
            Assert.That(message.BasicProperties.CorrelationId, Is.EqualTo(observedEnvelope.CausalityId));
            Assert.That(message.Exchange, Is.EqualTo(subject));
            Assert.That(message.RoutingKey, Is.EqualTo(routingKey));
            Assert.That(Encoding.UTF8.GetString(message.Body.ToArray()), Is.EqualTo(observedEnvelope.Payload));
            Assert.That(ReadHeader(message.BasicProperties.Headers, "tenant"), Is.EqualTo("backend"));
        });

        await DeleteTopologyAsync(queueName, subject);
    }

    [Test]
    public async Task Inlet_Consumes_Message_And_Forwards_Envelope_To_Ingress()
    {
        var registry = new NotificationTypeRegistry();
        registry.Register<OrderCreatedEvent>();

        var address = registry.ResolveAddress(typeof(OrderCreatedEvent));
        var queueName = CreateQueueName("inlet-client", address.Subject, address.Topic);
        var routingKey = CreateRouteKeyFromTopic(address.Topic);
        var messageId = Guid.NewGuid().ToString("N");
        var causalityId = Guid.NewGuid().ToString("N");
        var payload = "{\"value\":84}";
        var receivedEnvelope = new TaskCompletionSource<NotificationEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var inlet = new RabbitMqNotificationInlet(
            CreateOptions("inlet-client"),
            NullLogger<RabbitMqNotificationInlet>.Instance,
            registry);

        await inlet.Open(envelope =>
        {
            receivedEnvelope.TrySetResult(envelope);
            return Task.FromResult(EInletResponse.Ack);
        });

        await Task.Delay(500, CancellationToken.None);

        await using var publishConnection = await CreateFactory().CreateConnectionAsync();
        await using var publishChannel = await publishConnection.CreateChannelAsync();
        await publishChannel.ExchangeDeclarePassiveAsync(address.Subject);
        await publishChannel.QueueDeclarePassiveAsync(queueName);

        var properties = new BasicProperties
        {
            MessageId = messageId,
            CorrelationId = causalityId,
            Headers = new Dictionary<string, object?>
            {
                ["tenant"] = Encoding.UTF8.GetBytes("backend")
            }
        };

        await publishChannel.BasicPublishAsync(
            address.Subject,
            routingKey,
            false,
            properties,
            Encoding.UTF8.GetBytes(payload));

        var envelopeFromInlet = await receivedEnvelope.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(envelopeFromInlet.MessageId, Is.EqualTo(messageId));
            Assert.That(envelopeFromInlet.CausalityId, Is.EqualTo(causalityId));
            Assert.That(envelopeFromInlet.Subject, Is.EqualTo(address.Subject));
            Assert.That(envelopeFromInlet.Topic, Is.EqualTo(address.Topic));
            Assert.That(envelopeFromInlet.Payload, Is.EqualTo(payload));
            Assert.That(envelopeFromInlet.Headers["tenant"], Is.EqualTo("backend"));
            Assert.That(envelopeFromInlet.Headers["route-key"], Is.EqualTo(routingKey));
        });

        //await DeleteTopologyAsync(queueName, address.Subject);
    }

    private RabbitMqNotificationOptions CreateOptions(string clientName)
    {
        return new RabbitMqNotificationOptions
        {
            HostName = HostName,
            Port = _port,
            UserName = UserName,
            Password = Password,
            ClientName = clientName,
            Durable = false,
            AutoDelete = true,
            PrefetchCount = 1
        };
    }

    private ConnectionFactory CreateFactory()
    {
        return new ConnectionFactory
        {
            HostName = HostName,
            Port = _port,
            UserName = UserName,
            Password = Password,
            AutomaticRecoveryEnabled = true
        };
    }

    private async Task<int> TryResolveRabbitMqPortAsync()
    {
        foreach (var port in new[] { 15672, 5672 })
        {
            try
            {
                await using var connection = await new ConnectionFactory
                {
                    HostName = HostName,
                    Port = port,
                    UserName = UserName,
                    Password = Password,
                    AutomaticRecoveryEnabled = true
                }.CreateConnectionAsync();

                if (connection.IsOpen)
                {
                    return port;
                }
            }
            catch
            {
            }
        }

        return 0;
    }

    private async Task DeleteTopologyAsync(string queueName, string subject)
    {
        try
        {
            await using var connection = await CreateFactory().CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeleteAsync(queueName, false, false);
            await channel.ExchangeDeleteAsync(subject, false);
        }
        catch
        {
        }
    }

    private static async Task<BasicGetResult?> WaitForMessageAsync(Func<Task<BasicGetResult?>> messageReader)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var message = await messageReader();
            if (message != null)
            {
                return message;
            }

            await Task.Delay(250);
        }

        return null;
    }

    private static string CreateQueueName(string clientName, string subject, string topic)
    {
        return string.Join('.', new[] { clientName, NormalizeSegment(subject), CreateRouteKeyFromTopic(topic) }
            .Where(segment => !string.IsNullOrWhiteSpace(segment)));
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

    private static string ReadHeader(IDictionary<string, object?> headers, string key)
    {
        return headers[key] switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            string value => value,
            null => string.Empty,
            var value => value.ToString() ?? string.Empty
        };
    }

    private sealed class OrderCreatedEvent
    {
        public string Value { get; set; }
    }
}
