using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

public sealed class InMemoryNotificationHandlingStore : INotificationHandlingStore
{
    private readonly ConcurrentDictionary<string, byte> _handledNotifications = new();

    public Task<bool> TryMarkAsHandled(string notificationKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handledNotifications.TryAdd(notificationKey, 0));
    }

    public Task UnmarkAsHandled(string notificationKey, CancellationToken cancellationToken)
    {
        _handledNotifications.TryRemove(notificationKey, out _);
        return Task.CompletedTask;
    }
}
