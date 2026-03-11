using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteractR.Interactor;
using InteractR.Notifications;
using InteractR.Resolver;
using InteractR.Tests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace InteractR.Tests;

[TestFixture]
public class SelfContainedResolverTests
{
    private SelfContainedResolver _resolver;

    [SetUp]
    public void Setup()
    {
        _resolver = new SelfContainedResolver();
    }

    [Test]
    public void Can_Register_Interactor()
    {
        _resolver.Register(new MockInteractor());
    }

    [Test]
    public void Can_Register_Middleware()
    {
        _resolver.Register(Substitute.For<IMiddleware<MockUseCase, IMockOutputPort>>());
        var middleware = _resolver.ResolveMiddleware<MockUseCase, IMockOutputPort>(new MockUseCase());

        Assert.That(middleware != null);
    }

    [Test]
    public void Can_Resolve_Middleware()
    {
        var middleware = Substitute.For<IMiddleware<IHasPolicy>>();
        _resolver.Register(middleware);

        _resolver.ResolveMiddleware<MockUseCase>().FirstOrDefault()
            .Execute(new MockUseCase(), null, CancellationToken.None);

        middleware.Received(1).Execute(Arg.Any<MockUseCase>(),
            Arg.Any<Func<MockUseCase, CancellationToken?, Task<UseCaseResult>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void CanResolve_Interactor_ByInterface()
    {
        var interactor = new MockInteractor();
        _resolver.Register(interactor);
        Assert.DoesNotThrow(() =>
        {
            var resolvedInteractor = _resolver.ResolveInteractor<MockUseCase, IMockOutputPort>(new MockUseCase());
            resolvedInteractor.Execute(new MockUseCase(), new MockOutputPort(), CancellationToken.None);
        });
    }

    [Test]
    public void Can_Register_And_Resolve_NotificationHandlers()
    {
        var notificationHandler = Substitute.For<INotificationHandler<MockNotification>>();
        _resolver.Register(notificationHandler);

        var handlers = _resolver.ResolveNotificationHandlers<MockNotification>();

        Assert.That(handlers.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Register_And_Resolve_NotificationOutlets()
    {
        var outlet = Substitute.For<INotificationOutlet>();
        _resolver.Register(outlet);

        var outlets = _resolver.ResolveNotificationOutlets();

        Assert.That(outlets.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Register_And_Resolve_NotificationInlets()
    {
        var inlet = Substitute.For<INotificationInlet>();
        _resolver.Register(inlet);

        var inlets = _resolver.ResolveNotificationInlets();

        Assert.That(inlets.Count, Is.EqualTo(1));
    }
}