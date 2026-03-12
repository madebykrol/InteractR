namespace InteractR.Notifications;

public interface INotificationMetaDataResolver
{
    NotificationMetaData Resolve(object notification);
}
