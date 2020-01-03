# InteractR
[![Build Status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/Interactor-CI?branchName=master)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=7&branchName=master)

**Inspired by the ideas from "clean architecture" and MediatR.**

InteractR is used as a way to create a clean separation between the client and the domain / business logic.

Install from nuget.
```PowerShell
PM > Install-Package InteractR -Version 2.0.0
```

## Howto: Interactor

### Usecase

```csharp
class GreetUseCase : IUseCase<IGreetUseCaseOutputPort> {
	public string Name {get;}
	public GreetUseCase(name) {
		if(string.IsNullOrEmpty(name)
			throw new ArgumentException();
			
		Name = name;
	}
}
```

### Interactor

```csharp
class GreetUseCaseInteractor : IInteractor<GreetUseCase, IGreetUseCaseOutputPort> 
{
	public Task<UseCaseResult> Execute(GreetUseCase useCase, IGreetUseCaseOutputPort outputPort, CancellationToken cancellationToken)
	{
		outputPort.DisplayGreeting($"Hello, {useCase.Name}");
		
		return Task.FromResult(new UseCaseResult(true));
	}
}
```

### Usage Console App


```csharp
public class ConsoleOutput : IGreetUseCaseOutputPort {
	public void DisplayGreeting(string message) {
		Console.WriteLine(message);
	}
}
```

```csharp

// Registration
var resolver = new SelfContainedResolver();
resolver.Register(new GreetUseCaseInteractor());

var interactorHub = new Hub(_resolver);

var console = new ConsoleOutput();

await interactorHub.Execute(new GreetUseCase("John Doe"), (IGreetUseCaseOutputPort) console);
// Would display Hello, John Doe in a console application.
```

### Usage MVC

```csharp
public class GreetingPagePresenter : IGreetUseCaseOutputPort, IGreetingPagePresenter {

	private string _greeting;

	public void DisplayGreeting(string message) {
		_greeting = message;
	}

	...

	public GreetingPageViewModel Present() {
		var viewModel = new GreetingPageViewModel();
		viewModel.Greeting = _greeting;

		return viewModel;
	}
}
```
Registration and execution

```csharp

// Registration
var resolver = new SelfContainedResolver();
resolver.Register(new GreetUseCaseInteractor());

var interactorHub = new Hub(_resolver);

var presenter = new GreetingPagePresenter();

await interactorHub.Execute(new GreetUseCase("John Doe"), (IGreetUseCaseOutputPort) presenter);

return View(presenter.Present());
```

## Howto: Pipeline
InteractR supports a middleware pipeline from 2.0.0 that allowes developers to control the flow of what happends before, after or if at all a interactor executes.

Middleware can perform tasks related to a use case before an interactor executes or after, it can also terminate the pipeline. The letter might be usefull if for example some conditions are not met
or a feature-flag is set to off.

As interactors don't produce a return model for what to be displayed, Middlewares cannot manipulate the output directly. 
However as the OutputPort is part of the method signature the output methods can be called.


### Register middleware
```csharp
public class FooMiddleware : IMiddleware<FooUseCase, IFooOutputPort> {
	public  Task<UseCaseResult> Execute(FooUseCase usecase, IFooOutputPort outputPort, Func<FooUseCase, Task<UseCaseResult>> next, CancellationToken cancellationToken) {
		// Do some stuff before interactor

		next.Invoke(usecase); // remove this to terminate the pipeline.

		// Do some stuff after interactor
	}
}
```

```csharp
var resolver = new SelfContainedResolver();
resolver.Register(new FooMiddleWare());
```

Or you can register the middleware with any Dependency Injection Container and use either a provided resolver or roll your own.

## Resolvers
Autofac - [InteractR.Resolver.Autofac](https://github.com/madebykrol/InteractR.Resolver.Autofac) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.AutoFac)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=11) <br />
Ninject - [InteractR.Resolver.Ninject](https://github.com/madebykrol/InteractR.Resolver.Ninject) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.Ninject)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=10) <br />
StructureMap- [InteractR.Resolver.StructureMap](https://github.com/madebykrol/InteractR.Resolver.StructureMap) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.StructureMap)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=12)

## Roadmap
- [x] Execute Use Case Interactor.
- [x] Support for pipelines to enable feature flagging / feature toggling.
- [ ] Support for Global "Catch all" Middleware in a usecase pipeline
- [ ] "Assembly scan" resolver that will auto register interactors in the assemblies.
- [ ] Add more "Dependency Injection Container" Resolvers.
