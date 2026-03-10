using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

public interface INotificationOutlet
{
    Task Open(CancellationToken cancellationToken);
    Task Publish<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken)
        where TNotification : INotification;
}
