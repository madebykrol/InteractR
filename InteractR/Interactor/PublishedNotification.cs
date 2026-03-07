using System;

namespace InteractR.Interactor;

public sealed class PublishedNotification<TNotification>
    where TNotification : INotification
{
    public PublishedNotification(TNotification notification, string messageId = null, NotificationOrigin origin = NotificationOrigin.InProcess)
    {
        Notification = notification ?? throw new ArgumentNullException(nameof(notification));
        MessageId = string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString("N") : messageId;
        Origin = origin;
    }

    public string MessageId { get; }
    public TNotification Notification { get; }
    public NotificationOrigin Origin { get; }
}
