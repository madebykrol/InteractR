using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

public interface INotificationHandlingStore
{
    Task<bool> TryMarkAsHandled(string notificationKey, CancellationToken cancellationToken);
    Task UnmarkAsHandled(string notificationKey, CancellationToken cancellationToken);
}
