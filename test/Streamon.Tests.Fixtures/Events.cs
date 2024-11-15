namespace Streamon.Tests.Fixtures;

public record OrderCaptured(string Id, string Product, decimal Price);

public record OrderConfirmed(string Id, string By);

public record OrderShipped(string Id, string Tracking);

public record OrderArchived(string Id);

public record OrderFulfilled(string Id);

public record OrderCancelled(string Id);

public static class OrderEvents
{
    public readonly static OrderCaptured OrderCaptured = new("1", "Computer", 1000);
    public readonly static OrderConfirmed OrderConfirmed = new("1", "joe");
    public readonly static OrderShipped OrderShipped = new("1", "T1234567890");
    public readonly static OrderArchived OrderArchived = new("1");
    public readonly static OrderFulfilled OrderFulfilled = new("1");
    public readonly static OrderCancelled OrderCancelled = new("1");
}