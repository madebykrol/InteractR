using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Notifications;

public interface INotificationHub
{
    Task OpenInlets(CancellationToken cancellationToken = default, EProcessingStrategy strategy = EProcessingStrategy.Sequential);
    Task OpenOutlets(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publish a notification to in-process handlers and / or out-of-process subscribers through outlets.
    ///     Notifications will be routed based on naming conventions
    /// </summary>
    /// <typeparam name="TNotification"></typeparam>
    /// <param name="notification"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publish a notification to in-process handlers and / or out-of-process subscribers through outlets with explicit subject, topic and optional headers for more advanced scenarios like filtering, routing, etc.
    /// </summary>
    /// <typeparam name="TNotification"></typeparam>
    /// <param name="notification"></param>
    /// <param name="subject"></param>
    /// <param name="topic"></param>
    /// <param name="headers"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Publish<TNotification>(TNotification notification, string subject, string topic, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Register handler with implicit subject and topic based on the notification type for simple scenarios.
    /// </summary>
    /// <typeparam name="TNotification"></typeparam>
    /// <typeparam name="THandler"></typeparam>
    void RegisterHandler<TNotification, THandler>() where THandler : INotificationHandler<TNotification>;

    /// <summary>
    ///     Register handler with explicit subject, topic and optional headers for more advanced scenarios like filtering, routing, etc.
    /// </summary>
    /// <typeparam name="TNotification"></typeparam>
    /// <typeparam name="THandler"></typeparam>
    /// <param name="subject"></param>
    /// <param name="topic"></param>
    /// <param name="headers"></param>
    void RegisterHandler<TNotification, THandler>(string subject, string topic, IDictionary<string, string>? headers = null) where THandler : INotificationHandler<TNotification>;
}