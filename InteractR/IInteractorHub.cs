using InteractR.Interactor;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR
{
    public interface IInteractorHub
    {
        Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>;
        Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>;
    }
}