using System;

namespace InteractorHub.Notification
{
    public interface INotification
    {
        DateTime Sent { get; set; }
        object  Sender { get; set; }
    }
}
