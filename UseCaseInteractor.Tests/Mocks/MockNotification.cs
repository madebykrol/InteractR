using System;
using System.Collections.Generic;
using System.Text;
using UseCaseMediator.Notification;

namespace UseCaseMediator.Tests.Mocks
{
    public class MockNotification : INotification
    {
        public string Message { get; set; }
        public DateTime Sent { get; set; }
        public object Sender { get; set; }
    }
}
