using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public interface INotificationInlet
{
    Task Open(Func<NotificationEnvelope, Task<EInletResponse>> ingress, CancellationToken cancellationToken = default,
        PublishStrategy strategy = PublishStrategy.Sequential);
}

public class NotificationEnvelope
{
    public string MessageId { get; set; }
    public string Payload { get; set; }
    public string Address { get; set; }
    public string Headers { get; set; }
}

public enum EInletResponse
{
    Ack,
    Nack
}

public enum ENotificationResponse
{
    Ignored,
    Completed,
    Failed
}