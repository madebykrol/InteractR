using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task<ENotificationResponse> Handle(TNotification notification, CancellationToken cancellationToken);
}
