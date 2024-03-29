﻿using InteractR.Interactor;

namespace InteractR.Resolver;

public interface IRegistrator
{
    void Register<TUseCase, TOutputPort>(IInteractor<TUseCase, TOutputPort> interactor)
        where TUseCase : IUseCase<TOutputPort>;

    void Register<TUseCase, TOutputPort>(IMiddleware<TUseCase, TOutputPort> middleware)
        where TUseCase : IUseCase<TOutputPort>;

    void Register(IMiddleware middleware);
}