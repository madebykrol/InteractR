using System.Collections.Generic;

namespace InteractR.Notifications;

public sealed class NotificationMetaData
{
    public string MessageId { get; set; }
    public string CausalityId { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
