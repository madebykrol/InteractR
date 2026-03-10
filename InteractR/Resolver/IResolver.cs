using InteractR.Interactor;
using System.Collections.Generic;

namespace InteractR.Resolver;

public interface IResolver
{
    IInteractor<TUseCase, TOutputPort> ResolveInteractor<TUseCase, TOutputPort>(TUseCase useCase)
        where TUseCase : IUseCase<TOutputPort>;
    IReadOnlyList<IMiddleware<TUseCase, TOutputPort>> ResolveMiddleware<TUseCase, TOutputPort>(TUseCase useCase)
        where TUseCase : IUseCase<TOutputPort>;

    IReadOnlyList<IMiddleware<TUseCase>> ResolveMiddleware<TUseCase>();
    IReadOnlyList<IMiddleware> ResolveGlobalMiddleware();
    IReadOnlyList<INotificationHandler<TNotification>> ResolveNotificationHandlers<TNotification>()
        where TNotification : INotification;
    IReadOnlyList<INotificationOutlet> ResolveNotificationOutlets();
    IReadOnlyList<INotificationInlet> ResolveNotificationInlets();
}