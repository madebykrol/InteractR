using System;
using InteractorHub.Notification;

namespace InteractorHub.Tests.Mocks
{
    public class MockNotification : INotification
    {
        public string Message { get; set; }
        public DateTime Sent { get; set; }
        public object Sender { get; set; }
    }
}
