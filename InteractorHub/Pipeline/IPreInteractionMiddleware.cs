using System;

namespace InteractorHub.Pipeline
{
    public interface IPreInteractionMiddleware<TRequest>
    {
        void Intercept(TRequest request, Action<TRequest> next);
    }

    public interface IPreInteractionMiddleware
    {
        void Intercept();
    }
}