using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteractR.Interactor
{
    public interface IInteractor<in TUseCase, in TOutputPort> where TUseCase : IUseCase<TOutputPort>
    {
        Task<UseCaseResult> Execute(TUseCase usecase, TOutputPort outputPort, CancellationToken cancellationToken);
    }
}
