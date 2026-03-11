using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Notifications;

public interface INotificationHub
{
    Task OpenNotificationInlets(CancellationToken cancellationToken = default, EProcessingStrategy strategy = EProcessingStrategy.Sequential);
    Task OpenNotificationOutlets(CancellationToken cancellationToken = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default);
}