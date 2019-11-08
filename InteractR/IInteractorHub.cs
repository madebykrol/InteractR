using System.Threading.Tasks;
using InteractorHub.Interactor;

namespace InteractorHub
{
    public interface IInteractorHub
    {
        Task<TResponse> Execute<TResponse, TRequest>(TRequest query) where TRequest : IInteractionRequest<TResponse>;
    }
}