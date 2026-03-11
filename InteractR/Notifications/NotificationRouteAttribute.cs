using System;

namespace InteractR.Notifications;

/// <summary>
/// Explicitly declares the Subject and Topic for routing a notification/event
/// through outlets and inlets. When absent, the resolver falls back to an
/// implicit convention (namespace ? Subject, type name ? Topic).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NotificationRouteAttribute : Attribute
{
    public string Subject { get; }
    public string Topic { get; }

    public NotificationRouteAttribute(string subject, string topic)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
    }
}
