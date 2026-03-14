using InteractR.Exceptions;
using InteractR.Interactor;
using InteractR.Notifications;
using InteractR.Resolver;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public class Hub : IHub
{
    private readonly IResolver _resolver;
    private readonly INotificationTypeRegistry _typeRegistry;
    private readonly INotificationMetaDataResolver _notificationMetaDataResolver;
    private readonly IHubOptions _options;
    private readonly ILogger<Hub> _logger;
    private readonly List<NotificationHandlerRegistration> _handlerRegistrations = [];

    public Hub(IResolver resolver, INotificationTypeRegistry typeRegistry, IHubOptions hubOptions, ILogger<Hub> logger)
        : this(resolver, typeRegistry, new NotificationMetaDataMap(), hubOptions, logger)
    {
    }

    public Hub(IResolver resolver, INotificationTypeRegistry typeRegistry, INotificationMetaDataResolver notificationMetaDataResolver, ILogger<Hub> logger)
        : this(resolver, typeRegistry, notificationMetaDataResolver, new HubOptions(), logger)
    {
    }

    public Hub(IResolver resolver, INotificationTypeRegistry typeRegistry, INotificationMetaDataResolver notificationMetaDataResolver, IHubOptions hubOptions, ILogger<Hub> logger)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        _notificationMetaDataResolver = notificationMetaDataResolver ?? throw new ArgumentNullException(nameof(notificationMetaDataResolver));
        _options = hubOptions ?? throw new ArgumentNullException(nameof(hubOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort)
        where TUseCase : IUseCase<TOutputPort>
        => Execute(useCase, outputPort, CancellationToken.None);

    public Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken)
        where TUseCase : IUseCase<TOutputPort>
    {
        if (useCase == null)
        {
            throw new UseCaseNullException("The usecase cannot be null");
        }

        if (outputPort == null)
        {
            throw new OutputPortNullException("The output port cannot be null");
        }

        var interactor = _resolver.ResolveInteractor<TUseCase, TOutputPort>(useCase);
        var pipeline = new List<IMiddleware<TUseCase, TOutputPort>>();

        pipeline
            .AddRange(_resolver.ResolveGlobalMiddleware().Select(x => new GlobalMiddlewareWrapper<TUseCase, TOutputPort>(x)));
        pipeline
            .AddRange(_resolver.ResolveMiddleware<TUseCase>().Select(x => new MiddlewareWrapper<TUseCase, TOutputPort>(x)));
        pipeline
            .AddRange(_resolver.ResolveMiddleware<TUseCase, TOutputPort>(useCase).ToList());

        if (pipeline.Count == 0)
        {
            return interactor.Execute(useCase, outputPort, cancellationToken);
        }

        pipeline = pipeline
            .OrderBy(x => x is IOrdered orderedMiddleware ? orderedMiddleware.Order : 0)
            .ToList();

        pipeline.Add(new InteractorMiddlewareWrapper<TUseCase, TOutputPort>(interactor));

        var currentMiddleWare = 0;
        Task<UseCaseResult> NextMiddleWare(TUseCase usecase, CancellationToken? cancellationTokenOverride = null)
        {
            var effectiveCancellationToken = cancellationTokenOverride ?? cancellationToken;
            while (currentMiddleWare < pipeline.Count)
            {
                var middleware = pipeline[currentMiddleWare++];
                if (middleware is IConditionalMiddleware<TUseCase> conditionalMiddleware && !conditionalMiddleware.ShouldExecute(usecase))
                {
                    continue;
                }

                return middleware.Execute(usecase, outputPort, NextMiddleWare, effectiveCancellationToken);
            }

            return Task.FromResult(new UseCaseResult(true));
        }

        return NextMiddleWare(useCase);
    }

    public Task OpenInlets(CancellationToken cancellationToken = default, EProcessingStrategy strategy = EProcessingStrategy.Sequential)
    {
        var inlets = _resolver.ResolveNotificationInlets();
        if (inlets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            inlets.Select(x =>
                x.Open(envelope => SendEnvelopeToHandlers(envelope, cancellationToken),
                    cancellationToken,
                    strategy)));
    }

    /// <summary>
    ///     Opens all registered outlets and starts publishing notifications to them. This should typically be called once during application startup.
    ///     How outlets handle the published notifications (e.g. which transport they use, how they serialize the notifications, etc.) is up to the implementation of the outlet.
    /// </summary>
    public Task OpenOutlets(CancellationToken cancellationToken = default)
    {
        var outlets = _resolver.ResolveNotificationOutlets();

        if (outlets.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(outlets.Select(x => x.Open(cancellationToken)));
    }

    public void RegisterHandler<TNotification, THandler>() where THandler : INotificationHandler<TNotification>
    {
        var address = _typeRegistry.ResolveAddress(typeof(TNotification));
        RegisterHandler<TNotification, THandler>(address.Subject, address.Topic, null);
    }

    public void RegisterHandler<TNotification, THandler>(string subject, string topic, IDictionary<string, string>? headers = null) where THandler : INotificationHandler<TNotification>
    {
        var normalizedSubject = NormalizeSubject(subject);
        var normalizedTopic = NormalizeTopic(topic);

        _typeRegistry.Register(typeof(TNotification), normalizedSubject, normalizedTopic);
        _typeRegistry.RegisterSubscription(typeof(TNotification), typeof(THandler));

        if (_handlerRegistrations.Any(r =>
                r.NotificationType == typeof(TNotification) &&
                r.HandlerType == typeof(THandler) &&
                string.Equals(r.Subject, normalizedSubject, StringComparison.Ordinal) &&
                string.Equals(r.Topic, normalizedTopic, StringComparison.Ordinal)))
        {
            return;
        }

        _handlerRegistrations.Add(new NotificationHandlerRegistration(
            typeof(TNotification),
            typeof(THandler),
            normalizedSubject,
            normalizedTopic,
            headers == null ? null : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)));
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
    {
        var address = _typeRegistry.ResolveAddress(typeof(TNotification));
        return Publish(notification, address.Subject, address.Topic, null, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, string subject, string topic, IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = ResolveMetaData(notification);

        if (headers != null)
        {
            metadata.Headers = MergeHeaders(metadata.Headers, headers);
        }

        var address = new NotificationAddress
        {
            Subject = NormalizeSubject(subject),
            Topic = NormalizeTopic(topic)
        };

        return PublishInternal(notification, metadata, address, cancellationToken);
    }

    private static IDictionary<string, string> MergeHeaders(IDictionary<string, string> metaDataHeaders, IDictionary<string, string> providedHeaders)
    {
        var merged = new Dictionary<string, string>(metaDataHeaders, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in providedHeaders)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    private NotificationMetaData ResolveMetaData<TNotification>(TNotification notification)
    {
        var metaData = _notificationMetaDataResolver.Resolve(notification) ?? new NotificationMetaData();

        if (string.IsNullOrWhiteSpace(metaData.MessageId))
        {
            metaData.MessageId = Guid.NewGuid().ToString();
        }

        metaData.Headers ??= new Dictionary<string, string>();
        return metaData;
    }

    private async Task<EInletResponse> SendEnvelopeToHandlers(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            var registrations = ResolveRegistrations(envelope.Subject, envelope.Topic);
            if (registrations.Count == 0)
            {
                return EInletResponse.Nack;
            }

            var overallResult = ENotificationResponse.Ignored;
            foreach (var registration in registrations)
            {
                var notification = DeserializeNotification(envelope.Payload, registration.NotificationType);
                if (notification == null)
                {
                    continue;
                }

                var response = await InvokeHandlerByRegistration(registration, notification, cancellationToken).ConfigureAwait(false);
                if (response == ENotificationResponse.Failed)
                {
                    overallResult = ENotificationResponse.Failed;
                }
                else if (response == ENotificationResponse.Completed && overallResult != ENotificationResponse.Failed)
                {
                    overallResult = ENotificationResponse.Completed;
                }
            }

            return overallResult == ENotificationResponse.Failed ? EInletResponse.Nack : EInletResponse.Ack;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return EInletResponse.Nack;
        }
    }

    private async Task PublishInternal<TNotification>(TNotification notification, NotificationMetaData metaData, NotificationAddress address, CancellationToken cancellationToken)
    {
        var registrations = ResolveRegistrations(address.Subject, address.Topic)
            .Where(r => r.NotificationType == typeof(TNotification))
            .ToList();

        foreach (var registration in registrations)
        {
            await InvokeHandlerByRegistration(registration, notification!, cancellationToken).ConfigureAwait(false);
        }

        var envelope = new NotificationEnvelope
        {
            MessageId = metaData.MessageId,
            CausalityId = metaData.CausalityId,
            Headers = metaData.Headers,
            Subject = address.Subject,
            Topic = address.Topic,
            Payload = JsonSerializer.Serialize(notification)
        };

        var outlets = _resolver.ResolveNotificationOutlets();
        foreach (var outlet in outlets)
        {
            await outlet.Publish(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    private IReadOnlyList<NotificationHandlerRegistration> ResolveRegistrations(string subject, string topic)
    {
        var normalizedSubject = NormalizeSubject(subject);
        var normalizedTopic = NormalizeTopic(topic);

        return _handlerRegistrations
            .Where(r => RoutePatternMatches(r.Subject, normalizedSubject) && RoutePatternMatches(r.Topic, normalizedTopic))
            .ToList();
    }

    private async Task<ENotificationResponse> InvokeHandlerByRegistration(NotificationHandlerRegistration registration, object notification, CancellationToken cancellationToken)
    {
        var invokeMethod = typeof(Hub)
            .GetMethod(nameof(InvokeTypedHandler), BindingFlags.Instance | BindingFlags.NonPublic)
            ?.MakeGenericMethod(registration.NotificationType, registration.HandlerType);

        if (invokeMethod == null)
        {
            return ENotificationResponse.Failed;
        }

        var task = (Task<ENotificationResponse>?)invokeMethod.Invoke(this, [notification, cancellationToken]);
        if (task == null)
        {
            return ENotificationResponse.Failed;
        }

        return await task.ConfigureAwait(false);
    }

    private async Task<ENotificationResponse> InvokeTypedHandler<TNotification, THandler>(object notification, CancellationToken cancellationToken)
        where THandler : INotificationHandler<TNotification>
    {
        var typedNotification = notification is TNotification casted
            ? casted
            : (TNotification)notification;

        var handlers = _resolver.ResolveNotificationHandlers<TNotification>()
            .Where(h => h is THandler)
            .ToList();

        if (handlers.Count == 0)
        {
            if (Activator.CreateInstance(typeof(THandler)) is not THandler fallbackHandler)
            {
                return ENotificationResponse.Ignored;
            }

            return await fallbackHandler.Handle(typedNotification, cancellationToken).ConfigureAwait(false);
        }

        return await PublishSequential(typedNotification, handlers, cancellationToken).ConfigureAwait(false);
    }

    public Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort> => Execute(useCase, outputPort);

    public Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort> => Execute(useCase, outputPort, cancellationToken);

    private static async Task<ENotificationResponse> PublishSequential<TNotification>(
        TNotification notification,
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
    {
        if (handlers.Count == 0)
        {
            return ENotificationResponse.Ignored;
        }

        var overallResult = ENotificationResponse.Completed;
        foreach (var handler in handlers)
        {
            var result = await handler.Handle(notification, cancellationToken).ConfigureAwait(false);

            if (result == ENotificationResponse.Failed)
            {
                overallResult = ENotificationResponse.Failed;
            }
        }

        return overallResult;
    }

    private static object? DeserializeNotification(object? payload, Type notificationType)
    {
        if (payload == null)
        {
            return null;
        }

        if (notificationType.IsInstanceOfType(payload))
        {
            return payload;
        }

        if (payload is JsonElement element)
        {
            return element.Deserialize(notificationType);
        }

        if (payload is string json)
        {
            return JsonSerializer.Deserialize(json, notificationType);
        }

        return JsonSerializer.Deserialize(JsonSerializer.Serialize(payload), notificationType);
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

    private static bool RoutePatternMatches(string pattern, string value)
    {
        var patternParts = SplitRoute(pattern);
        var valueParts = SplitRoute(value);
        return MatchRoute(patternParts, 0, valueParts, 0);
    }

    private static bool MatchRoute(string[] patternParts, int patternIndex, string[] valueParts, int valueIndex)
    {
        if (patternIndex == patternParts.Length)
        {
            return valueIndex == valueParts.Length;
        }

        var pattern = patternParts[patternIndex];

        if (pattern == "#")
        {
            if (patternIndex == patternParts.Length - 1)
            {
                return true;
            }

            for (var i = valueIndex; i <= valueParts.Length; i++)
            {
                if (MatchRoute(patternParts, patternIndex + 1, valueParts, i))
                {
                    return true;
                }
            }

            return false;
        }

        if (valueIndex >= valueParts.Length)
        {
            return false;
        }

        if (pattern == "*" || string.Equals(pattern, valueParts[valueIndex], StringComparison.OrdinalIgnoreCase))
        {
            return MatchRoute(patternParts, patternIndex + 1, valueParts, valueIndex + 1);
        }

        return false;
    }

    private static string[] SplitRoute(string route)
        => string.IsNullOrWhiteSpace(route)
            ? []
            : route
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .ToArray();


    private sealed class NotificationHandlerRegistration
    {
        public NotificationHandlerRegistration(
            Type notificationType,
            Type handlerType,
            string subject,
            string topic,
            IReadOnlyDictionary<string, string>? headers)
        {
            NotificationType = notificationType;
            HandlerType = handlerType;
            Subject = subject;
            Topic = topic;
            Headers = headers;
        }

        public Type NotificationType { get; }
        public Type HandlerType { get; }
        public string Subject { get; }
        public string Topic { get; }
        public IReadOnlyDictionary<string, string>? Headers { get; }
    }
}


public interface IHubOptions
{
    Func<object, NotificationRouteData>? ResolveNotificationRouteData { get; set; }
}

public class HubOptions : IHubOptions
{
    public Func<object, NotificationRouteData>? ResolveNotificationRouteData { get; set; }
}

public class NotificationRouteData
{
    public string RouteKey { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}