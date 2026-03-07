using InteractR.Interactor;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public interface INotificationIngress
{
    Task Ingest<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken = default,
        PublishStrategy strategy = PublishStrategy.Sequential)
        where TNotification : INotification;
}
