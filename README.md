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

Implement `IOrdered` to control execution order:

```csharp
public sealed class AuditMiddleware : IMiddleware<GreetUseCase, IGreetUseCaseOutputPort>, IOrdered
{
    public int Order => 10;

    public Task<UseCaseResult> Execute(
        GreetUseCase useCase,
        IGreetUseCaseOutputPort outputPort,
        Func<GreetUseCase, CancellationToken?, Task<UseCaseResult>> next,
        CancellationToken cancellationToken)
        => next(useCase, null);
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
        Func<GreetUseCase, CancellationToken?, Task<UseCaseResult>> next,
        CancellationToken cancellationToken)
        => next(useCase, null);
}
```

## Notifications: in-process + out-of-process

InteractR supports:

- plain CLR event types
- in-process handlers (`INotificationHandler<TNotification>`)
- out-of-process outlets (`INotificationOutlet`) for event bus publishing
- inbound adapters (`INotificationInlet`) registered as message sources
- implicit address resolution based on type names or explicit routing via `NotificationRouteAttribute`

### Define and register event handlers/outlets

```csharp
public sealed class UserRegistered
{
    public Guid UserId { get; }
    
    public UserRegistered(Guid userId) => UserId = userId;
}

public sealed class WelcomeEmailHandler : INotificationHandler<UserRegistered>
{
    public Task<ENotificationResponse> Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        // Send email
        return Task.FromResult(ENotificationResponse.Completed);
    }
}

public sealed class BrokerOutlet : INotificationOutlet
{
    public Task Open(CancellationToken cancellationToken) => Task.CompletedTask;
    
    public Task Publish(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        // Push envelope (with Subject, Topic, MessageId, and JSON payload) to broker
        return Task.CompletedTask;
    }
}

public sealed class BrokerInlet : INotificationInlet
{
    public Task Open(
        Func<NotificationEnvelope, Task<EInletResponse>> ingress,
        CancellationToken cancellationToken = default,
        EProcessingStrategy strategy = EProcessingStrategy.Sequential)
    {
        // Example broker callback when receiving message:
        // var envelope = new NotificationEnvelope {
        //     MessageId = brokerMessageId,
        //     Subject = "User",
        //     Topic = "Registered",
        //     Payload = jsonPayload,
        //     Headers = new Dictionary<string, string>()
        // };
        // var response = await ingress(envelope);
        // if (response == EInletResponse.Ack) BrokerAck(); else BrokerNack();
        return Task.CompletedTask;
    }
}

var resolver = new SelfContainedResolver();
resolver.Register(new WelcomeEmailHandler());
resolver.Register(new BrokerOutlet());
resolver.Register(new BrokerInlet());

var hub = new Hub(resolver);
```

By convention, `UserRegistered` resolves to `Subject = "user"` and `Topic = "registered"`.
`OrderLineCreatedEvent` resolves to `Subject = "order"` and `Topic = "line.created"`.
Use `NotificationRouteAttribute` when you want explicit routing:

```csharp
[NotificationRoute("Identity", "UserRegistered")]
public sealed class UserRegisteredIntegrationEvent
{
    public Guid UserId { get; }

    public UserRegisteredIntegrationEvent(Guid userId) => UserId = userId;
}
```

### Publish from inside the process

```csharp
await hub.Publish(new UserRegistered(Guid.NewGuid()));
```

This will:
1. Run in-process handlers.
2. Resolve a `Subject` and `Topic` for the event type.
3. Serialize the event to JSON and forward it to registered outlets.

### Custom notification metadata mapping (without domain dependency on InteractR)

You can inject `INotificationMetaDataResolver` directly into `Hub` and map your own event contracts (for example `IEvent`) to transport metadata.

```csharp
public interface IEvent
{
    string Id { get; }
    string CorrelationId { get; }
}

var resolver = new SelfContainedResolver();
var metadataMap = new NotificationMetaDataMap()
    .Map<IEvent>(e => new NotificationMetaData
    {
        MessageId = e.Id,
        CausalityId = e.CorrelationId
    });

var hub = new Hub(
    resolver,
    resolver.NotificationTypeRegistry,
    metadataMap,
    new HubOptions());
```

This keeps domain events as plain CLR types and avoids adding InteractR-specific interfaces in domain/core.

### Start inbound ingestion

```csharp
await hub.OpenNotificationInlets(cancellationToken);
```

Inlets are source adapters. The hub ingests from all registered inlets, deserializes JSON payloads, resolves the event type from `Subject` and `Topic`, routes to in-process handlers, and does not re-publish out-of-process-origin messages to outlets.

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
