using System;

namespace InteractR.Notifications;

/// <summary>
/// Maintains the mapping between notification routing addresses (Subject/Topic)
/// and CLR types. Populated at startup, queried at dispatch time.
/// </summary>
public interface INotificationTypeRegistry
{
    /// <summary>
    /// Registers a notification type and its resolved routing address.
    /// </summary>
    void Register(Type notificationType);

    /// <summary>
    /// Registers a notification type and its resolved routing address.
    /// </summary>
    void Register<TNotification>();

    /// <summary>
    /// Resolves the CLR type for the given Subject/Topic pair.
    /// Returns null when no type has been registered for that address.
    /// </summary>
    Type Resolve(string subject, string topic);

    /// <summary>
    /// Resolves the routing address for a given notification type.
    /// Uses <see cref="NotificationRouteAttribute"/> when present,
    /// otherwise falls back to namespace ? Subject, type name ? Topic.
    /// </summary>
    NotificationAddress ResolveAddress(Type notificationType);
}
