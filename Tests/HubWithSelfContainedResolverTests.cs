using System.Threading;
using InteractR.Interactor;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests;

[TestFixture]
public class HubWithSelfContainedResolverTests
{
    private IInteractorHub _interactorHub;
    private SelfContainedResolver _handlerResolver;
    private IInteractor<MockUseCase, IMockOutputPort> _mockUseCaseInteractor;
    private ILogger<Hub> _logger;

    [SetUp]
    public void Setup()
    {
        _handlerResolver = new SelfContainedResolver();
        _logger = Substitute.For<ILogger<Hub>>();
        _interactorHub = new Hub(_handlerResolver, _handlerResolver.NotificationTypeRegistry, new HubOptions(), _logger);
    }

    [Test]
    public void TestUseCaseDispatcher()
    {
        _mockUseCaseInteractor = Substitute.For<IInteractor<MockUseCase, IMockOutputPort>>();
        _handlerResolver.Register(new MockMiddleware());
        _handlerResolver.Register(_mockUseCaseInteractor);
        _interactorHub.Execute(new MockUseCase(), new MockOutputPort(), CancellationToken.None);
        _mockUseCaseInteractor.Received().Execute(Arg.Any<MockUseCase>(), Arg.Any<IMockOutputPort>(), Arg.Any<CancellationToken>());
    }
}