using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;
using InteractR.Resolver;

namespace InteractR
{
    public sealed class Hub : IInteractorHub
    {
        private readonly IResolver _resolver;

        public Hub(IResolver resolver)
        {
            _resolver = resolver;
        }

        public Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort) where TUseCase : IUseCase<TOutputPort>
            => Execute(useCase, outputPort, CancellationToken.None);

        public Task<UseCaseResult> Execute<TUseCase, TOutputPort>(TUseCase useCase, TOutputPort outputPort, CancellationToken cancellationToken) where TUseCase : IUseCase<TOutputPort>
            => _resolver.ResolveInteractor<TUseCase, TOutputPort>(useCase).Execute(useCase, outputPort, cancellationToken);
    }
}
