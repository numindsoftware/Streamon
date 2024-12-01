using Streamon.Tests.Fixtures;

namespace Streamon.Subscription.Tests;

public class StreamSubscriptionTests
{
    private readonly CheckpointStore _checkpointStore = new();
    private readonly EventHandlerResolver _eventHandlerResolver = new();
    private readonly SubscriptionStreamReader _subscriptionStreamReader = new();

    public StreamSubscriptionTests()
    {
        _subscriptionStreamReader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(1), StreamPosition.From(1), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCaptured));
        _subscriptionStreamReader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(2), StreamPosition.From(2), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderConfirmed));
        _subscriptionStreamReader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(3), StreamPosition.From(3), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderShipped));
        _subscriptionStreamReader.Events.Add(new Event(StreamId.From("order-123"), EventId.New(), StreamPosition.From(4), StreamPosition.From(4), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderFulfilled));
        _subscriptionStreamReader.Events.Add(new Event(StreamId.From("order-124"), EventId.New(), StreamPosition.From(1), StreamPosition.From(5), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCaptured));
        _subscriptionStreamReader.Events.Add(new Event(StreamId.From("order-124"), EventId.New(), StreamPosition.From(2), StreamPosition.From(6), DateTimeOffset.Now, BatchId.New(), OrderEvents.OrderCancelled));
    }

    [Fact]
    public async Task EventHandlerCaptured()
    {
        StreamSubscription subscription = new(new SubscriptionId("test-subscription"), StreamSubscriptionType.CatchUp, _eventHandlerResolver, _checkpointStore, _subscriptionStreamReader);
        subscription.AddEventHandler<TestEventHandler>();
        await subscription.PollAsync();
    }
}

