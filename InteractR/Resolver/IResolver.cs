using System;
using System.Collections.Generic;

namespace InteractorHub.Resolver
{
    public interface IResolver
    {
        TInteractor ResolveInteractor<TInteractor>();
        object ResolveInteractor(Type interactorType);
    }
}
