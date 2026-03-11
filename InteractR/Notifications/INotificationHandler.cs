using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Notifications;

/// <summary>
/// Handler for processing notifications/events.
/// Can handle any event type - INotification implementation is optional.
/// </summary>
/// <typeparam name="TNotification">Type of notification/event to handle</typeparam>
public interface INotificationHandler<TNotification>
{
    Task<ENotificationResponse> Handle(TNotification notification, CancellationToken cancellationToken);
}

