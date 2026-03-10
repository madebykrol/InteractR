using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor;

public interface IMiddleware<TUseCase, in TOutputPort> 
    where TUseCase : IUseCase<TOutputPort>
{
    Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, Func<TUseCase, CancellationToken?, Task<UseCaseResult>> next, CancellationToken cancellationToken);
}

public interface IOrdered
{
    int Order { get; }
}

public interface IConditionalMiddleware<in TUseCase>
{
    bool ShouldExecute(TUseCase usecase);
}

public interface IMiddleware
{
    Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, CancellationToken?, Task<UseCaseResult>> next, CancellationToken cancellationToken);
}

public interface IMiddleware<in TType>
{
    Task<UseCaseResult> Execute<TUseCase>(TUseCase usecase, Func<TUseCase, CancellationToken?, Task<UseCaseResult>> next,
        CancellationToken cancellationToken)
        where TUseCase :  TType;
}