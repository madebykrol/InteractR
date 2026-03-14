using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InteractR.Notifications;

public sealed class NotificationTypeRegistry : INotificationTypeRegistry
{
    private readonly Dictionary<(string Subject, string Topic), Type> _map = new();
    private readonly Dictionary<Type, HashSet<Type>> _subscriptions = new();
    private readonly Dictionary<Type, NotificationAddress> _typeToAddress = new();

    public void Register(Type notificationType)
    {
        var address = ResolveAddressInternal(notificationType);
        Register(notificationType, address.Subject, address.Topic);
    }

    public void Register(Type notificationType, string subject, string topic)
    {
        var normalizedSubject = NormalizeSubject(subject);
        var normalizedTopic = NormalizeTopic(topic);

        _map[(normalizedSubject, normalizedTopic)] = notificationType;
        _typeToAddress[notificationType] = new NotificationAddress
        {
            Subject = normalizedSubject,
            Topic = normalizedTopic
        };
    }

    public void Register<TNotification>()
        => Register(typeof(TNotification));

    public void RegisterSubscription(Type notificationType, Type handlerType)
    {
        var handlerInterface = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        if (!handlerInterface.IsAssignableFrom(handlerType))
        {
            throw new InvalidOperationException($"Handler type {handlerType.Name} does not implement INotificationHandler<{notificationType.Name}>.");
        }

        if (!_subscriptions.ContainsKey(notificationType))
        {
            _subscriptions[notificationType] = new HashSet<Type>();
        }

        _subscriptions[notificationType].Add(handlerType);

        if (!_typeToAddress.ContainsKey(notificationType))
        {
            Register(notificationType);
        }
    }

    public void RegisterSubscription<TNotification, THandler>()
        where THandler : INotificationHandler<TNotification>
        => RegisterSubscription(typeof(TNotification), typeof(THandler));

    public Type? Resolve(string subject, string topic)
    {
        var key = (NormalizeSubject(subject), NormalizeTopic(topic));
        return _map.TryGetValue(key, out var type)
            ? type
            : null;
    }

    public NotificationAddress ResolveAddress(Type notificationType)
    {
        if (_typeToAddress.TryGetValue(notificationType, out var explicitAddress))
        {
            return explicitAddress;
        }

        return ResolveAddressInternal(notificationType);
    }

    private static NotificationAddress ResolveAddressInternal(Type notificationType)
    {
        var attr = notificationType.GetCustomAttribute<NotificationRouteAttribute>();
        if (attr != null)
        {
            var attrSubject = notificationType.Namespace ?? string.Empty;
            var attrTopic = notificationType.Name;

            if (!string.IsNullOrEmpty(attr.Subject))
            {
                attrSubject = attr.Subject;
            }

            if (!string.IsNullOrEmpty(attr.Topic))
            {
                attrTopic = attr.Topic;
            }

            if (!string.IsNullOrEmpty(attr.Subject) || !string.IsNullOrEmpty(attr.Topic))
            {
                return new NotificationAddress
                {
                    Subject = NormalizeSubject(attrSubject),
                    Topic = NormalizeTopic(attrTopic)
                };
            }
        }

        var splitTypeName = SplitByCapitalLetters(notificationType.Name);
        if (splitTypeName.Length > 0)
        {
            var subject = splitTypeName[0];
            var topic = ToDotNotation(splitTypeName.Skip(1).ToArray());

            return new NotificationAddress
            {
                Subject = NormalizeSubject(subject),
                Topic = NormalizeTopic(topic)
            };
        }

        return new NotificationAddress
        {
            Subject = NormalizeSubject(notificationType.Namespace ?? string.Empty),
            Topic = NormalizeTopic(notificationType.Name)
        };
    }

    private static string ToDotNotation(string[] topic)
        => string.Join(".", topic.Select(x => x.ToLowerInvariant()));

    public IReadOnlyList<Type> NotificationSubscriptionTypes()
    {
        return _subscriptions.Keys.ToList();
    }

    private static string[] SplitByCapitalLetters(string notificationTypeName)
    {
        if (notificationTypeName == null)
        {
            return [];
        }

        var words = Regex.Split(notificationTypeName, @"(?=\p{Lu})", RegexOptions.Compiled)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (words.Length > 1 &&
            (words[^1].Equals("Notification", StringComparison.OrdinalIgnoreCase) ||
             words[^1].Equals("Event", StringComparison.OrdinalIgnoreCase)))
        {
            Array.Resize(ref words, words.Length - 1);
        }

        return words;
    }

    private static string NormalizeSubject(string subject)
        => (subject ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return string.Empty;
        }

        var value = topic.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (value.Contains('.', StringComparison.Ordinal) || value.Contains('*', StringComparison.Ordinal) || value.Contains('#', StringComparison.Ordinal))
        {
            return value.Trim('.').ToLowerInvariant();
        }

        var words = Regex.Split(value, @"(?=\p{Lu})", RegexOptions.Compiled)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant());

        return string.Join('.', words);
    }
}
