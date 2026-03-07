using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;

namespace InteractR.Tests.Mocks;

public sealed class MockNotificationHandler : INotificationHandler<MockNotification>
{
    public bool Executed { get; private set; }

    public Task Handle(MockNotification notification, CancellationToken cancellationToken)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}
