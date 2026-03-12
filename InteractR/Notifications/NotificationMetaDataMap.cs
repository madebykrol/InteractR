using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace InteractR.Notifications;

public sealed class NotificationMetaDataMap : INotificationMetaDataResolver
{
    private readonly List<(Type Type, Func<object, NotificationMetaData> Resolver)> _mappings = new();
    private readonly ConcurrentDictionary<Type, Func<object, NotificationMetaData>> _cache = new();

    public NotificationMetaDataMap Map<TNotification>(Func<TNotification, NotificationMetaData> resolver)
    {
        if (resolver == null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        _mappings.Add((typeof(TNotification), x => resolver((TNotification)x)));
        _cache.Clear();
        return this;
    }

    public NotificationMetaData Resolve(object notification)
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        var resolver = _cache.GetOrAdd(notification.GetType(), BuildResolverFor);
        return resolver(notification);
    }

    private Func<object, NotificationMetaData> BuildResolverFor(Type concreteType)
    {
        for (var i = _mappings.Count - 1; i >= 0; i--)
        {
            if (_mappings[i].Type == concreteType)
            {
                return _mappings[i].Resolver;
            }
        }

        for (var i = _mappings.Count - 1; i >= 0; i--)
        {
            if (_mappings[i].Type.IsAssignableFrom(concreteType))
            {
                return _mappings[i].Resolver;
            }
        }

        return x => ResolveByShape(x, concreteType);
    }

    private static NotificationMetaData ResolveByShape(object notification, Type notificationType)
    {
        var messageId =
            ReadString(notification, notificationType, "MessageId") ??
            ReadString(notification, notificationType, "Id") ??
            ReadString(notification, notificationType, "EventId");

        var causalityId =
            ReadString(notification, notificationType, "CausalityId");

        var correlationId =
            ReadString(notification, notificationType, "CorrelationId") ??
            ReadString(notification, notificationType, "DiagnosticsId");

        var sentAt = DateTime.UtcNow;
        DateTime.TryParse(ReadString(notification, notificationType, "SentAt"), out sentAt);


        var headers =
            ReadHeaders(notification, notificationType, "Headers") ??
            new Dictionary<string, string>();

        return new NotificationMetaData
        {
            MessageId = messageId,
            CausalityId = causalityId,
            CorrelationId = correlationId,
            SentAt = sentAt,
            Headers = headers
        };
    }

    private static string ReadString(object notification, Type notificationType, string propertyName)
    {
        var property = notificationType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property?.CanRead != true || property.PropertyType != typeof(string))
        {
            return null;
        }

        return (string)property.GetValue(notification);
    }

    private static IDictionary<string, string> ReadHeaders(object notification, Type notificationType, string propertyName)
    {
        var property = notificationType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property?.CanRead != true)
        {
            return null;
        }

        if (property.GetValue(notification) is IDictionary<string, string> dictionary)
        {
            return new Dictionary<string, string>(dictionary);
        }

        return null;
    }
}
