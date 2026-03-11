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
            Assert.That(address.Subject, Is.EqualTo("Order"));
            Assert.That(address.Topic, Is.EqualTo("Placed"));
        });
    }

    [Test]
    public void ResolveAddress_Splits_Notification_Name_Into_Subject_And_Topic()
    {
        var address = _registry.ResolveAddress(typeof(UserCreatedNotification));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("User"));
            Assert.That(address.Topic, Is.EqualTo("Created"));
        });
    }

    [Test]
    public void ResolveAddress_Joins_All_But_Last_Word_Into_Subject()
    {
        var address = _registry.ResolveAddress(typeof(OrderLineItemCreatedEvent));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo("OrderLineItem"));
            Assert.That(address.Topic, Is.EqualTo("Created"));
        });
    }

    [Test]
    public void ResolveAddress_Uses_Empty_Subject_For_Single_Word_Name()
    {
        var address = _registry.ResolveAddress(typeof(MockNotification));

        Assert.Multiple(() =>
        {
            Assert.That(address.Subject, Is.EqualTo(string.Empty));
            Assert.That(address.Topic, Is.EqualTo("Mock"));
        });
    }

    [Test]
    public void Register_Uses_Resolved_Address_For_Lookup()
    {
        _registry.Register<OrderPlacedEvent>();

        var resolvedType = _registry.Resolve("Order", "Placed");

        Assert.That(resolvedType, Is.EqualTo(typeof(OrderPlacedEvent)));
    }

    [NotificationRoute("billing", "captured")]
    private sealed class RoutedNotification;

    private sealed class UserCreatedNotification;

    private sealed class OrderLineItemCreatedEvent;
}
