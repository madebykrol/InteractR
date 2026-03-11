using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Notifications;

/// <summary>
/// Represents an inlet that receives notifications from external message brokers
/// and delivers them to in-process handlers via the Hub.
/// </summary>
/// <remarks>
/// Inlets handle broker-specific message reception and create NotificationEnvelope
/// instances with serialized payloads. The Hub deserializes and routes to handlers.
/// </remarks>
public interface INotificationInlet
{
    /// <summary>
    /// Opens the inlet to start receiving messages from the broker.
    /// </summary>
    /// <param name="ingress">Callback function to deliver received messages to the Hub</param>
    /// <param name="cancellationToken">Cancellation token to stop the inlet</param>
    /// <param name="strategy">Strategy for processing messages (Sequential or Parallel)</param>
    /// <returns>Task that completes when the inlet is closed</returns>
    /// <remarks>
    /// When developing out-of-process inlets the ignress-lambda should be stored so that it can be recreated in case of broker restarts or outages.
    /// </remarks>
    Task Open(Func<NotificationEnvelope, Task<EInletResponse>> ingress, CancellationToken cancellationToken = default,
        EProcessingStrategy strategy = EProcessingStrategy.Sequential);
}

/// <summary>
/// Response from Hub to inlet indicating whether to acknowledge or reject the message
/// </summary>
public enum EInletResponse
{
    /// <summary>
    /// Message processed successfully, acknowledge to broker
    /// </summary>
    Ack,
    
    /// <summary>
    /// Message processing failed, reject/nack to broker for retry or dead-letter
    /// </summary>
    Nack
}