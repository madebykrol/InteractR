# InteractR
[![Build Status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/Interactor-CI?branchName=master)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=7&branchName=master)

**Inspired by the ideas from "clean architecture" and MediatR.**

InteractR is used as a way to create a clean separation between the client and the domain / business logic.

Install from nuget.
```PowerShell
PM > Install-Package InteractR -Version 10.0.0
```

## Quick start: Use case + Interactor

### 1) Define a use case and output port

```csharp
public interface IGreetUseCaseOutputPort
{
    void DisplayGreeting(string message);
}

public sealed class GreetUseCase : IUseCase<IGreetUseCaseOutputPort>
{
    public GreetUseCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }
}
```

### 2) Implement an interactor

```csharp
public sealed class GreetUseCaseInteractor : IInteractor<GreetUseCase, IGreetUseCaseOutputPort>
{
    public Task<UseCaseResult> Execute(
        GreetUseCase useCase,
        IGreetUseCaseOutputPort outputPort,
        CancellationToken cancellationToken)
    {
        outputPort.DisplayGreeting($"Hello, {useCase.Name}");
        return Task.FromResult(new UseCaseResult(true));
    }
}
```

### 3) Register and execute

```csharp
public sealed class ConsoleOutput : IGreetUseCaseOutputPort
{
    public void DisplayGreeting(string message) => Console.WriteLine(message);
}

var resolver = new SelfContainedResolver();
resolver.Register(new GreetUseCaseInteractor());

var hub = new Hub(resolver);
var output = new ConsoleOutput();

await hub.Execute(new GreetUseCase("John Doe"), output);
await hub.Run(new GreetUseCase("Jane Doe"), output);
```

## Pipeline examples

InteractR supports global, generic, and use-case specific middleware.

### Ordered middleware

Implement `IOrderedMiddleware` to control execution order:

```csharp
public sealed class AuditMiddleware : IMiddleware<GreetUseCase, IGreetUseCaseOutputPort>, IOrderedMiddleware
{
    public int Order => 10;

    public Task<UseCaseResult> Execute(
        GreetUseCase useCase,
        IGreetUseCaseOutputPort outputPort,
        Func<GreetUseCase, Task<UseCaseResult>> next,
        CancellationToken cancellationToken)
        => next(useCase);
}
```

### Conditional middleware

Implement `IConditionalMiddleware<TUseCase>` to skip middleware when needed:

```csharp
public sealed class FeatureToggleMiddleware : IMiddleware<GreetUseCase, IGreetUseCaseOutputPort>, IConditionalMiddleware<GreetUseCase>
{
    private readonly IFeatureFlags _featureFlags;

    public FeatureToggleMiddleware(IFeatureFlags featureFlags)
    {
        _featureFlags = featureFlags;
    }

    public bool ShouldExecute(GreetUseCase useCase)
        => _featureFlags.IsEnabled("greet");

    public Task<UseCaseResult> Execute(
        GreetUseCase useCase,
        IGreetUseCaseOutputPort outputPort,
        Func<GreetUseCase, Task<UseCaseResult>> next,
        CancellationToken cancellationToken)
        => next(useCase);
}
```

## Notifications: in-process + out-of-process

InteractR supports:

- in-process handlers (`INotificationHandler<TNotification>`)
- out-of-process outlets (`INotificationOutlet`) for event bus publishing
- inbound adapters (`INotificationInlet`) registered as message sources
- idempotent handling using `INotificationHandlingStore` (default: `InMemoryNotificationHandlingStore`)

### Define and register notification handlers/outlets

```csharp
public sealed class UserRegistered : INotification
{
    public UserRegistered(Guid userId) => UserId = userId;

    public Guid UserId { get; }
}

public sealed class WelcomeEmailHandler : INotificationHandler<UserRegistered>
{
    public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        // Send email
        return Task.CompletedTask;
    }
}

public sealed class BrokerOutlet : INotificationOutlet
{
    public Task Publish<TNotification>(PublishedNotification<TNotification> notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        // Push notification.MessageId + payload to broker
        return Task.CompletedTask;
    }
}

public sealed class BrokerInlet : INotificationInlet
{
    public Task Start(INotificationIngress ingress, CancellationToken cancellationToken = default, PublishStrategy strategy = PublishStrategy.Sequential)
    {
        // Example broker callback:
        // return ingress.Ingest(new PublishedNotification<UserRegistered>(payload, messageId, NotificationOrigin.OutOfProcess), cancellationToken, strategy);
        return Task.CompletedTask;
    }
}

var resolver = new SelfContainedResolver();
resolver.Register(new WelcomeEmailHandler());
resolver.Register(new BrokerOutlet());
resolver.Register(new BrokerInlet());

var hub = new Hub(resolver);
```

### Publish from inside the process

```csharp
await hub.Publish(new UserRegistered(Guid.NewGuid()));
```

This will:
1. Run in-process handlers.
2. Forward the same event (with message id) to registered outlets.

### Start inbound ingestion (broker -> inlet -> hub)

```csharp
await hub.StartNotificationInlets(cancellationToken);
```

Inlets are source adapters. The hub ingests from all registered inlets, routes to in-process handlers, and does not re-publish out-of-process-origin notifications to outlets (prevents loops).

## Pluggable idempotency store (inbox/outbox scenarios)

Use a custom `INotificationHandlingStore` when you need durable deduplication (for example SQL/Redis):

```csharp
public sealed class SqlNotificationHandlingStore : INotificationHandlingStore
{
    public async Task<bool> TryMarkAsHandled(string notificationKey, CancellationToken cancellationToken)
    {
        // Insert key with unique constraint; return false if already exists
        return await Task.FromResult(true);
    }

    public Task UnmarkAsHandled(string notificationKey, CancellationToken cancellationToken)
    {
        // Optional rollback/remove when publish fails
        return Task.CompletedTask;
    }
}

var hub = new Hub(resolver, new SqlNotificationHandlingStore());
```

## Resolvers

Autofac - [InteractR.Resolver.Autofac](https://github.com/madebykrol/InteractR.Resolver.Autofac) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.AutoFac)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=11)  
Ninject - [InteractR.Resolver.Ninject](https://github.com/madebykrol/InteractR.Resolver.Ninject) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.Ninject)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=10)  
StructureMap - [InteractR.Resolver.StructureMap](https://github.com/madebykrol/InteractR.Resolver.StructureMap) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.StructureMap)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=12)  
Lamar - [InteractR.Resolver.Lamar](https://github.com/madebykrol/InteractR.Resolver.Lamar) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.Lamar)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=12)

## Roadmap
- [x] Execute Use Case Interactor.
- [x] Support for pipelines to enable feature flagging / feature toggling.
- [x] Support for Global "Catch all" Middleware in a usecase pipeline
- [ ] "Assembly scan" resolver that will auto register interactors in the assemblies.
- [ ] Add more "Dependency Injection Container" Resolvers.
