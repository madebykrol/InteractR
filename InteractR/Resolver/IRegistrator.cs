using System;
using System.Collections.Generic;
using System.Text;
using InteractorHub.Interactor;

namespace InteractorHub.Resolver
{
    public interface IRegistrator
    {

        void Register<TRequest, TResponse>(IInteractor<TRequest, TResponse> interactor)
            where TRequest : IInteractionRequest<TResponse>;
    }
}
