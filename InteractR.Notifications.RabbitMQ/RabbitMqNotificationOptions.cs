namespace InteractR.Notifications.RabbitMQ;

public sealed class RabbitMqNotificationOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = global::RabbitMQ.Client.AmqpTcpEndpoint.UseDefaultPort;
    public string UserName { get; set; } = global::RabbitMQ.Client.ConnectionFactory.DefaultUser;
    public string Password { get; set; } = global::RabbitMQ.Client.ConnectionFactory.DefaultPass;
    public string VirtualHost { get; set; } = global::RabbitMQ.Client.ConnectionFactory.DefaultVHost;
    public string ClientName { get; set; } = "interactr";
    public string ExchangeType { get; set; } = global::RabbitMQ.Client.ExchangeType.Topic;
    public bool Durable { get; set; }
    public bool RequeueOnError { get; set; }
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; } = false;
    public bool AutoAck { get; set; } = false;
    public ushort PrefetchCount { get; set; } = 10;
}
