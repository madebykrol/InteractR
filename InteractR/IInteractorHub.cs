using InteractR.Interactor;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public interface IInteractorHub
{
    Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>;
    Task OpenNotificationInlets(CancellationToken cancellationToken = default, PublishStrategy strategy = PublishStrategy.Sequential);
    Task OpenNotificationOutlets(CancellationToken cancellationToken = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default, PublishStrategy strategy = PublishStrategy.Sequential)
        where TNotification : INotification;
    Task Publish<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken = default, PublishStrategy strategy = PublishStrategy.Sequential)
        where TNotification : INotification;
}