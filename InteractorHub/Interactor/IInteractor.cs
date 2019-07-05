using System.Threading;
using System.Threading.Tasks;

namespace InteractorHub.Interactor
{
    public interface IInteractor<TRequest, TResponse> where TRequest : IUseCaseRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
