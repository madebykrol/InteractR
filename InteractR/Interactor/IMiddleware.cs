using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

public interface IMiddleware<TUseCase, in TOutputPort> 
    where TUseCase : IUseCase<TOutputPort>
{
    Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken);
}

public interface IMiddleware
{
    Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken);
}

public interface IMiddleware<in TType>
{
    Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, Task<UseCaseResult>> next,
        CancellationToken cancellationToken)
        where TUseCase :  TType;
}