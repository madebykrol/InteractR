using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Exceptions;
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
            if (useCase == null)
            {
                throw new UseCaseNullException("The usecase cannot be null");
            }

            if (outputPort == null)
            {
                throw new OutputPortNullException("The output port cannot be null");
            }

            var pipeline = _resolver.ResolveMiddleware<TUseCase, TOutputPort>(useCase).ToList();
            var pipelineRoot = pipeline.FirstOrDefault();
            var interactor = _resolver.ResolveInteractor<TUseCase, TOutputPort>(useCase);

            if (pipelineRoot == null)
            {
                return interactor.Execute(useCase, outputPort, cancellationToken);
            }

            pipeline.Add(new InteractorMiddlewareWrapper<TUseCase, TOutputPort>(interactor));

            var currentMiddleWare = 1;
            Task<UseCaseResult> NextMiddleWare(TUseCase useCase)
            {
                return pipeline[currentMiddleWare++].Execute(useCase, outputPort,  NextMiddleWare, cancellationToken);
            }
            
            return pipelineRoot.Execute(useCase, outputPort, NextMiddleWare, cancellationToken);
        }
    }
}
