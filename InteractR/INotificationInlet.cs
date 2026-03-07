using InteractR.Interactor;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public interface INotificationInlet
{
    Task Start(INotificationIngress ingress, CancellationToken cancellationToken = default,
        PublishStrategy strategy = PublishStrategy.Sequential);
}
