using InteractR.Notifications;
using InteractR.Tests.Mocks;
using NUnit.Framework;

namespace InteractR.Tests;

[TestFixture]
public class NotificationTypeRegistryTests
{
    private NotificationTypeRegistry _registry;

    [SetUp]
    public void Setup()
    {
        _registry = new NotificationTypeRegistry();
    }

    [Test]
    public void ResolveAddress_Uses_Attribute_When_Present()
    {
        var address = _registry.ResolveAddress(typeof(RoutedNotification));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("billing"));
            Assert.That(address.Topic, Is.EqualTo("captured"));
        });
    }

    [Test]
    public void ResolveAddress_Splits_Event_Name_Into_Subject_And_Topic()
    {
        var address = _registry.ResolveAddress(typeof(OrderPlacedEvent));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("order"));
            Assert.That(address.Topic, Is.EqualTo("placed"));
        });
    }

    [Test]
    public void ResolveAddress_Splits_Notification_Name_Into_Subject_And_Topic()
    {
        var address = _registry.ResolveAddress(typeof(UserCreatedNotification));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("user"));
            Assert.That(address.Topic, Is.EqualTo("created"));
        });
    }

    [Test]
    public void ResolveAddress_Joins_All_But_Last_Word_Into_Subject()
    {
        var address = _registry.ResolveAddress(typeof(OrderLineItemCreatedEvent));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("order"));
            Assert.That(address.Topic, Is.EqualTo("line.item.created"));
        });
    }

    [Test]
    public void ResolveAddress_Uses_First_Word_As_Subject_For_Single_Word_Name()
    {
        var address = _registry.ResolveAddress(typeof(MockNotification));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("mock"));
            Assert.That(address.Topic, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Register_Uses_Resolved_Address_For_Lookup()
    {
        _registry.Register<OrderPlacedEvent>();

        var resolvedType = _registry.Resolve("order", "placed");

        Assert.That(resolvedType, Is.EqualTo(typeof(OrderPlacedEvent)));
    }

    [Test]
    public void Resolve_Is_Case_Insensitive_For_Subject()
    {
        _registry.Register<OrderPlacedEvent>();

        var resolvedType = _registry.Resolve("ORDER", "placed");

        Assert.That(resolvedType, Is.EqualTo(typeof(OrderPlacedEvent)));
    }

    [Test]
    public void Resolve_Is_Case_Insensitive_For_Topic()
    {
        _registry.Register<OrderPlacedEvent>();

        var resolvedType = _registry.Resolve("order", "PLACED");

        Assert.That(resolvedType, Is.EqualTo(typeof(OrderPlacedEvent)));
    }

    [Test]
    public void Resolve_Is_Case_Insensitive_For_Mixed_Case_Subject_And_Topic()
    {
        _registry.Register<OrderLineItemCreatedEvent>();

        var resolvedType = _registry.Resolve("Order", "Line.Item.Created");

        Assert.That(resolvedType, Is.EqualTo(typeof(OrderLineItemCreatedEvent)));
    }

    [Test]
    public void Resolve_Handles_Null_Subject()
    {
        _registry.Register<OrderPlacedEvent>();

        var resolvedType = _registry.Resolve(null, "placed");

        Assert.That(resolvedType, Is.Null);
    }

    [Test]
    public void Resolve_Handles_Null_Topic()
    {
        _registry.Register<MockNotification>();

        var resolvedType = _registry.Resolve("mock", null);

        Assert.That(resolvedType, Is.EqualTo(typeof(MockNotification)));
    }

    [NotificationRoute("billing", "captured")]
    private sealed class RoutedNotification;

    private sealed class UserCreatedNotification;

    private sealed class OrderLineItemCreatedEvent;
}
