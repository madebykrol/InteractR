using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractorHub.Interactor;
using InteractorHub.Notification;
using InteractorHub.Resolver;

namespace InteractorHub
{
    public sealed class Hub : IHub
    {
        private readonly IResolver _resolver;

        public Hub(IResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<TResponse> Handle<TResponse, TRequest>(TRequest request) where TRequest : IInteractionRequest<TResponse>
        {
            var middleware = _resolver.ResolvePreInteractionMiddleWare<TRequest>();

            var result = await _resolver.ResolveInteractor<IInteractor<TRequest, TResponse>>().Handle(request, CancellationToken.None);

            return result;
        }
        
        public async Task Send<TNotification>(TNotification notification) where TNotification : INotification
        {
            foreach (var listener in _resolver.ResolveListeners<TNotification>())
            {
                await listener.Handle(notification, CancellationToken.None);
            }
        }

        private void NextInPipeline<TRequest>(TRequest request)
        {

        }
    }
}
