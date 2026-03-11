using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Notifications;

/// <summary>
/// Represents an outlet that publishes notifications to external message brokers.
/// Supports both plain domain events and events implementing INotification.
/// </summary>
/// <remarks>
/// Outlets receive domain notifications from the Hub and are responsible for:
/// - Serializing notifications to NotificationEnvelope (done automatically via default Publish implementation)
/// - Publishing envelopes to the external broker
/// - Handling broker-specific routing, durability, and delivery options
/// </remarks>
public interface INotificationOutlet
{
    /// <summary>
    /// Opens and initializes the connection to the message broker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Open(CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a notification to the broker. 
    /// Default implementation serializes the notification and calls PublishEnvelope.
    /// </summary>
    /// <param name="notification">Published notification with message ID and origin</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Publish(NotificationEnvelope notification, CancellationToken cancellationToken);

}



