using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InteractR.Notifications;

public sealed class NotificationTypeRegistry : INotificationTypeRegistry
{
    private readonly Dictionary<(string Subject, string Topic), Type> _map = new();

    public void Register(Type notificationType)
    {
        var address = ResolveAddress(notificationType);
        var key = (address.Subject, address.Topic);
        _map.TryAdd(key, notificationType);
    }

    public void Register<TNotification>()
        => Register(typeof(TNotification));

    public Type Resolve(string subject, string topic)
        => _map.TryGetValue((subject, topic), out var type) ? type : null;

    public NotificationAddress ResolveAddress(Type notificationType)
    {
        var attr = notificationType.GetCustomAttribute<NotificationRouteAttribute>();
        if (attr != null)
        {
            return new NotificationAddress { Subject = attr.Subject, Topic = attr.Topic };
        }

        // We should resolve Subject and Topic based on conventions

        // First we split the notification type name into words based on capital letters
        // For example, "UserCreatedNotification" would be split into ["User", "Created", "Notification"]
        // Clean up the name by removing "Notification" or "Event" suffix if it exists

        var splitTypeName = SplitByCapitalLetters(notificationType.Name);
        if (splitTypeName.Length > 0)
        {
            // The last word is the topic, and the rest is the subject
            var topic = splitTypeName[^1];
            var subject = string.Join("", splitTypeName, 0, splitTypeName.Length - 1);
            return new NotificationAddress { Subject = subject, Topic = topic };
        }

        return new NotificationAddress
        {
            Subject = notificationType.Namespace ?? string.Empty,
            Topic = notificationType.Name
        };
    }

    private string[] SplitByCapitalLetters(string notificationTypeName)
    {
        // split by case and remove "Notification" or "Event" suffix if it exists
        var words = Regex.Split(notificationTypeName, @"(?=\p{Lu})", RegexOptions.Compiled);

        // If last word is "Notification" or "Event", remove it
        if (words.Length > 1 && (words[^1].Equals("Notification", StringComparison.OrdinalIgnoreCase) || words[^1].Equals("Event", StringComparison.OrdinalIgnoreCase)))
        {
            Array.Resize(ref words, words.Length - 1);
        }

        return words;
    }
}
