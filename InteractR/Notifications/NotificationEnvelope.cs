using System.Collections.Generic;

namespace InteractR.Notifications;

/// <summary>
/// Envelope containing a serialized notification for transport between brokers and the Hub.
/// </summary>
public class NotificationEnvelope
{
    /// <summary>
    /// Unique identifier for the message, used for idempotent handling
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    ///     Unique identifier used to match causality
    /// </summary>
    public string CausalityId { get; set; }
    
    /// <summary>
    /// JSON-serialized notification payload
    /// </summary>
    public string Payload { get; set; }
    
    /// <summary>
    /// Represents the wide area of knowledge, the domain. In RabbitMQ this translates to an Exchange.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// The Specific aspects of the Subject that the notification treats
    /// </summary>
    public string Topic { get; set; }
    
    /// <summary>
    /// Optional metadata and headers.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; }
}