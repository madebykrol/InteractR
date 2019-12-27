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
        {
            var pipeline = _resolver.ResolveMiddleware<TUseCase, TOutputPort>(useCase);

            var interactor = _resolver.ResolveInteractor<TUseCase, TOutputPort>(useCase);

            var interactorMiddleware = new InteractorMiddlewareWrapper<TUseCase, TOutputPort>(interactor, outputPort);

            return interactor.Execute(useCase, outputPort, cancellationToken);
        }
    }
}
