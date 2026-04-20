namespace Streamon.Tests.Fixtures;

public static class OrderEvents
{
    public readonly static OrderCaptured OrderCaptured = new("1", new OrderAddress("123 Main St", "Cityville", "12345"), [new OrderItem("Computer", 1, 1000m)]);
    public readonly static OrderConfirmed OrderConfirmed = new("1", "joe");
    public readonly static OrderShipped OrderShipped = new("1", "T1234567890");
    public readonly static OrderArchived OrderArchived = new("1");
    public readonly static OrderFulfilled OrderFulfilled = new("1");
    public readonly static OrderCancelled OrderCancelled = new("1");
}

public record OrderCaptured(string Id, OrderAddress OrderAddress, List<OrderItem> Items);

public record OrderConfirmed(string Id, string By);

public record OrderShipped(string Id, string Tracking);

public record OrderArchived(string Id);

public record OrderFulfilled(string Id);

public record OrderCancelled(string Id);

/// <summary>
/// Event carrying complex types for projection testing.
/// </summary>
public record OrderDetailsUpdated(string Id, OrderAddress ShippingAddress, List<OrderItem> Items);

/// <summary>
/// Event for changing the shipping address of an order.
/// </summary>
public record OrderAddressChanged(string Id, OrderAddress ShippingAddress);

/// <summary>
/// Event for adding items to an order.
/// </summary>
public record OrderItemsAdded(string Id, List<OrderItem> Items);

/// <summary>
/// Event for cancelling specific items from an order by name.
/// </summary>
public record OrderItemsCancelled(string Id, List<string> ItemNames);

/// <summary>
/// Event for replacing all items in an order.
/// </summary>
public record OrderItemsReplaced(string Id, List<OrderItem> Items);
