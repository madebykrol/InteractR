using InteractR.Interactor;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR;

public interface IInteractorHub
{
    /// <summary>
    ///
    ///     Can possibly throw UseCaseNull- or OutputPortNullException
    /// </summary>
    /// <typeparam name="TUseCase"></typeparam>
    /// <typeparam name="TOutputPort"></typeparam>
    /// <param name="useCase"></param>
    /// <param name="outputPort"></param>
    /// <returns></returns>
    Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>;
    Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>;
}