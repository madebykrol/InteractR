# InteractR
[![Build Status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/Interactor-CI?branchName=master)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=7&branchName=master)

**Inspired by the ideas from "clean architecture" and MediatR.**

InteractR is used as a way to create a clean separation between the client and the domain / business logic.

Install from nuget.
```PowerShell
PM > Install-Package InteractR -Version 1.0.0
```

## Usage
Usecase

```csharp
class GreetUseCase : IUseCase<IGreetUseCaseOutputPort> {
	public string Name {get;}
	public GreetUseCase(input) {
		if(string.IsNullOrEmpty(name)
			throw new ArgumentException();
			
		Name = name;
	}
}
```

Interactor

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

Usage Console App


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

Usage MVC

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


```csharp

// Registration
var resolver = new SelfContainedResolver();
resolver.Register(new GreetUseCaseInteractor());

var interactorHub = new Hub(_resolver);

var presenter = new GreetingPagePresenter();

await interactorHub.Execute(new GreetUseCase("John Doe"), (IGreetUseCaseOutputPort) presenter);

return View(presenter.Present());
```

## Resolvers
Autofac - [InteractR.Resolver.Autofac](https://github.com/madebykrol/InteractR.Resolver.Autofac) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.AutoFac)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=11) <br />
Ninject - [InteractR.Resolver.Ninject](https://github.com/madebykrol/InteractR.Resolver.Ninject) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.Ninject)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=10) <br />
StructureMap- [InteractR.Resolver.StructureMap](https://github.com/madebykrol/InteractR.Resolver.StructureMap) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.StructureMap)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=12)

## Roadmap
- [x] Execute Use Case Interactor.
- [ ] "Assembly scan" resolver that will auto register interactors in the assemblies.
- [ ] Support for pipelines to enable feature flagging / feature toggling.
- [ ] Add more "Dependency Injection Container" Resolvers.
