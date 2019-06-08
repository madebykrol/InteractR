using System;

namespace UseCaseMediator.Notification
{
    public interface INotification
    {
        DateTime Sent { get; set; }
        object  Sender { get; set; }
    }
}
