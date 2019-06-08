using System.Threading;
using System.Threading.Tasks;

namespace UseCaseMediator.Interactor
{
    public interface IUseCaseInteractor<TRequest, TResponse> where TRequest : IUseCaseRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
