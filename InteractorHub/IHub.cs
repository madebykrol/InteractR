using System.Threading.Tasks;
using InteractorHub.Interactor;
using InteractorHub.Notification;

namespace InteractorHub
{
    public interface IHub
    {
        Task<TResponse> Handle<TResponse, TRequest>(TRequest query) where TRequest : IInteractionRequest<TResponse>;
        Task Send<TNotification>(TNotification notification) where TNotification : INotification;
    }
}