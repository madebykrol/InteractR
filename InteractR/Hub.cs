using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractorHub.Interactor;
using InteractorHub.Resolver;

namespace InteractorHub
{
    public sealed class Hub : IInteractorHub
    {
        private readonly IResolver _resolver;

        public Hub(IResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<TResponse> Execute<TResponse, TRequest>(TRequest request) where TRequest : IInteractionRequest<TResponse>
        {
            var result = await _resolver.ResolveInteractor<IInteractor<TRequest, TResponse>>().Handle(request, CancellationToken.None);

            return result;
        }
    }
}
