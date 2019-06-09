using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UseCaseMediator.Flow;
using UseCaseMediator.Interactor;
using UseCaseMediator.Notification;
using UseCaseMediator.Resolver;

namespace UseCaseMediator
{
    public sealed class UseCaseMediator : IUseCaseMediator
    {
        private readonly IResolver _resolver;

        public UseCaseMediator(IResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<TResponse> Handle<TResponse, TRequest>(TRequest request) where TRequest : IUseCaseRequest<TResponse>
        {
            var controllers = _resolver.ResolveFlowController<TRequest>();

            foreach (var controller in controllers.OrderBy(x => x.Weight))
            {
                if (!controller.Intercept(request))
                {
                    
                }
            }

            var result = await _resolver.ResolveInteractor<IUseCaseInteractor<TRequest, TResponse>>().Handle(request, CancellationToken.None);

            return result;
        }
        
        public async Task Send<TNotification>(TNotification notification) where TNotification : INotification
        {
            foreach (var listener in _resolver.ResolveListeners<TNotification>())
            {
                await listener.Handle(notification, CancellationToken.None);
            }
        }
    }
}
