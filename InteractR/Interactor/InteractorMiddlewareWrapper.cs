using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

internal sealed class InteractorMiddlewareWrapper<TUseCase, TOutputPort>
    : IMiddleware<TUseCase>
    where TUseCase : IUseCase<TOutputPort>
{
    private readonly IInteractor<TUseCase, TOutputPort> _interactor;
    private TOutputPort _outputPort;
    public InteractorMiddlewareWrapper(IInteractor<TUseCase, TOutputPort> interactor)
    {
        _interactor = interactor;
    }

    public InteractorMiddlewareWrapper<TUseCase, TOutputPort> SetOutputPort(TOutputPort outputPort)
    {
        _outputPort = outputPort;

        return this;
    }
    public Task<UseCaseResult> Execute<TUseCase1>(TUseCase1 usecase, Func<TUseCase1, Task<UseCaseResult>> next,
        CancellationToken cancellationToken) where TUseCase1 : TUseCase
        => _interactor.Execute(usecase, _outputPort, cancellationToken);
}