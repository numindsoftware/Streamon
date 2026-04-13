using Streamon.Tests.Fixtures;

namespace Streamon.Subscription.Tests;

public class EventHandlerTests
{
    [Fact]
    public async Task DispatchAsync_WhenCalled_DispatchesEvent()
    {
        var subscriptionId = SubscriptionId.New();
        var streamId = StreamId.From("order-123");
        EventHandlerContext<OrderShipped> context = new(subscriptionId, streamId, EventId.New(), StreamPosition.Start, StreamPosition.Start, DateTimeOffset.UtcNow, BatchId.New(), OrderEvents.OrderShipped);
        TestEventHandler eventHandler = new();
        await eventHandler.HandleAsync(context);
    }
}
