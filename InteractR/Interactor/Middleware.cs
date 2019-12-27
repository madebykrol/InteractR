using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor
{
    public interface IMiddleware<TUseCase, TOutputPort> 
        where TUseCase : IUseCase<TOutputPort>
    {
        Task<UseCaseResult> Execute(TUseCase usecase, IMiddleware<TUseCase, TOutputPort> next, CancellationToken cancellationToken);
    }
}
