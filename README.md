# InteractR
[![Build Status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/Interactor-CI?branchName=master)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=7&branchName=master)

## Usage
Usecase

```csharp
class GreetUseCase : IUseCase<IGreetUseCaseOutputPort> {
	public readonly string Name;
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

OutputPort 

```csharp
public class ConsoleOutput : IGreetUseCaseOutputPort {
	public void DisplayGreeting(string message) {
		Console.WriteLine(message);
	}
}
```

Usage

```csharp
_console = new ConsoleOutput();
await _interactorHub.Execute(new GreetUseCase("John Doe"), (IGreetUseCaseOutputPort) ConsoleOutput);
// Would display Hello, John Doe in a console application.
```

## Resolvers
Autofac - [InteractR.Resolver.Autofac](https://github.com/madebykrol/InteractR.Resolver.Autofac) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.AutoFac)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=11)

Ninject - [InteractR.Resolver.Ninject](https://github.com/madebykrol/InteractR.Resolver.Ninject) [![Build status](https://dev.azure.com/kristofferolsson/Interactor/_apis/build/status/InteractR.Resolver.Ninject)](https://dev.azure.com/kristofferolsson/Interactor/_build/latest?definitionId=10)

## Roadmap
- [x] Execute Use Case Interactor.
- [ ] Support for "Feature Flags".
- [ ] Add more "Dependency Injection Container" Resolvers
