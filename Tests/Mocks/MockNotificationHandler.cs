using System.Threading;
using System.Threading.Tasks;
using InteractR.Notifications;

namespace InteractR.Tests.Mocks;

public sealed class MockNotificationHandler : INotificationHandler<MockNotification>
{
    public bool Executed { get; private set; }

    public Task<ENotificationResponse> Handle(MockNotification notification, CancellationToken cancellationToken)
    {
        Executed = true;
        return Task.FromResult(ENotificationResponse.Completed);
    }
}
