using System.Threading.Tasks;
using UseCaseMediator.Flow;
using UseCaseMediator.Interactor;
using UseCaseMediator.Notification;

namespace UseCaseMediator
{
    public interface IUseCaseMediator
    {
        Task<TResponse> Handle<TResponse, TRequest>(TRequest query) where TRequest : IUseCaseRequest<TResponse>;
        Task Send<TNotification>(TNotification notification) where TNotification : INotification;
    }
}