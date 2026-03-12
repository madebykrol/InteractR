using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InteractR.Notifications;

public sealed class NotificationTypeRegistry : INotificationTypeRegistry
{
    private readonly Dictionary<(string Subject, string Topic), Type> _map = new();

    public void Register(Type notificationType)
    {
        var address = ResolveAddress(notificationType);
        var key = (address.Subject.ToLowerInvariant(), address.Topic.ToLowerInvariant());
        _map.TryAdd(key, notificationType);
    }

    public void Register<TNotification>()
        => Register(typeof(TNotification));

    public Type Resolve(string subject, string topic) =>
        _map.TryGetValue(
            (subject?.ToLowerInvariant() ?? string.Empty, ToDotNotation(SplitByCapitalLetters(topic))), out var type)
            ? type
            : null;

    public NotificationAddress ResolveAddress(Type notificationType)
    {
        var attr = notificationType.GetCustomAttribute<NotificationRouteAttribute>();
        if (attr != null)
        {
            var attrSubject = notificationType.Namespace ?? string.Empty;
            var attrTopic = notificationType.Name;

            if (!string.IsNullOrEmpty(attr.Subject))
                attrSubject = attr.Subject;

            if (!string.IsNullOrEmpty(attr.Topic))
                attrTopic = attr.Topic;

            if (!string.IsNullOrEmpty(attr.Subject) || !string.IsNullOrEmpty(attr.Topic))
            {
                return new NotificationAddress
                {
                    Subject = attrSubject,
                    Topic = attrTopic
                };
            }
        }

        var subject = notificationType.Namespace ?? string.Empty;
        var topic = notificationType.Name;

        // We should resolve Subject and Topic based on conventions

        // First we split the notification type name into words based on capital letters
        // For example, "UserCreatedNotification" would be split into ["User", "Created", "Notification"]
        // Clean up the name by removing "Notification" or "Event" suffix if it exists

        var splitTypeName = SplitByCapitalLetters(notificationType.Name);
        if (splitTypeName.Length > 0)
        {
            // The first word is the Subject and the rest is the Topic
            subject = splitTypeName[0].ToLower();
            topic = ToDotNotation(splitTypeName.Skip(1).ToArray());
        }

        return new NotificationAddress
        {
            Subject = subject,
            Topic = topic
        };
    }

    private static string ToDotNotation(string[] topic)
        => string.Join(".", topic.Select(x => x.ToLower()));

    public IReadOnlyList<Type> NotificationSubscriptionTypes()
    {
        return _map.Values.ToList();
    }

    private string[] SplitByCapitalLetters(string notificationTypeName)
    {
        if (notificationTypeName == null)
            return [];
        // split by case and remove "Notification" or "Event" suffix if it exists
        var words = Regex.Split(notificationTypeName, @"(?=\p{Lu})", RegexOptions.Compiled)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        // If last word is "Notification" or "Event", remove it
        if (words.Length > 1 && (words[^1].Equals("Notification", StringComparison.OrdinalIgnoreCase) || words[^1].Equals("Event", StringComparison.OrdinalIgnoreCase)))
        {
            Array.Resize(ref words, words.Length - 1);
        }

        return words;
    }
}
