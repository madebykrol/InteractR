using InteractR.Interactor;
using InteractR.Notifications;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public interface IInteractorHub
{
    Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Run<TUseCase, TOutputPort>(in TUseCase useCase, in TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>;
    
}

/// <summary>
/// IHub joins both types of Hub modes.
/// </summary>
public interface IHub : IInteractorHub, INotificationHub
{

}