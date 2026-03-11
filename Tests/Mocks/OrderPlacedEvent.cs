using System;

namespace InteractR.Tests.Mocks;

/// <summary>
/// Example of a plain domain event without INotification interface.
/// This demonstrates that InteractR can work with any type - no framework coupling required.
/// </summary>
public sealed class OrderPlacedEvent
{
    public OrderPlacedEvent(string orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
        PlacedAt = DateTime.UtcNow;
    }

    public string OrderId { get; }
    public decimal Amount { get; }
    public DateTime PlacedAt { get; }
}
