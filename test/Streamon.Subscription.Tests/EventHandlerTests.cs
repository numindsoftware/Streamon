using Streamon.Tests.Fixtures;

namespace Streamon.Subscription.Tests;

public class EventHandlerTests
{
    [Fact]
    public async Task DispatchAsync_WhenCalled_DispatchesEvent()
    {
        EventConsumeContext<object> context = new(SubscriptionId.New(), StreamId.From("order-123"), EventId.New(), OrderEvents.OrderCaptured);
        TestEventHandler eventHandler = new();
        await eventHandler.HandleAsync(context);
    }
}
