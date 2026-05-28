using Streamon.Tests.Fixtures;

namespace Streamon.Subscription.Tests;

public class InboxDeduplicationMiddlewareTests
{
    private static readonly SubscriptionId SubId = SubscriptionId.From("dedup-sub");
    private const string ConsumerName = "consumer-a";

    private static Event NewEvent(long position = 1) => new(
        StreamId.From("order-123"),
        EventId.New(),
        StreamPosition.From(position),
        StreamPosition.From(position),
        DateTimeOffset.UtcNow,
        BatchId.New(),
        OrderEvents.OrderCaptured);

    private sealed class FakeInbox : IEventInbox
    {
        public HashSet<EventId> Processed { get; } = [];
        public List<(SubscriptionId Sub, string Consumer, EventId EventId)> HasProcessedCalls { get; } = [];
        public List<(SubscriptionId Sub, string Consumer, EventId EventId)> MarkProcessedCalls { get; } = [];

        public Task<bool> HasProcessedAsync(SubscriptionId subscriptionId, string consumerName, EventId eventId, CancellationToken cancellationToken = default)
        {
            HasProcessedCalls.Add((subscriptionId, consumerName, eventId));
            return Task.FromResult(Processed.Contains(eventId));
        }

        public Task MarkProcessedAsync(SubscriptionId subscriptionId, string consumerName, Event @event, CancellationToken cancellationToken = default)
        {
            MarkProcessedCalls.Add((subscriptionId, consumerName, @event.EventId));
            Processed.Add(@event.EventId);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task FirstDeliveryInvokesNextAndMarksProcessed()
    {
        var inbox = new FakeInbox();
        var middleware = new InboxDeduplicationMiddleware(inbox, SubId, ConsumerName);
        var @event = NewEvent();
        var nextCalls = 0;

        await middleware.InvokeAsync(@event, (_, _) => { nextCalls++; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        Assert.Equal(1, nextCalls);
        Assert.Single(inbox.MarkProcessedCalls);
        Assert.Equal(@event.EventId, inbox.MarkProcessedCalls[0].EventId);
        Assert.Equal(ConsumerName, inbox.MarkProcessedCalls[0].Consumer);
    }

    [Fact]
    public async Task RedeliveryShortCircuitsAndDoesNotInvokeNext()
    {
        var inbox = new FakeInbox();
        var middleware = new InboxDeduplicationMiddleware(inbox, SubId, ConsumerName);
        var @event = NewEvent();
        inbox.Processed.Add(@event.EventId);
        var nextCalls = 0;

        await middleware.InvokeAsync(@event, (_, _) => { nextCalls++; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        Assert.Equal(0, nextCalls);
        Assert.Empty(inbox.MarkProcessedCalls);
    }

    [Fact]
    public async Task HandlerExceptionPreventsMarkProcessed()
    {
        var inbox = new FakeInbox();
        var middleware = new InboxDeduplicationMiddleware(inbox, SubId, ConsumerName);
        var @event = NewEvent();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(@event, (_, _) => throw new InvalidOperationException("boom"), TestContext.Current.CancellationToken));

        Assert.Empty(inbox.MarkProcessedCalls);
        Assert.DoesNotContain(@event.EventId, inbox.Processed);
    }

    [Fact]
    public async Task DistinctEventIdsAreAllProcessed()
    {
        var inbox = new FakeInbox();
        var middleware = new InboxDeduplicationMiddleware(inbox, SubId, ConsumerName);
        var first = NewEvent(1);
        var second = NewEvent(2);
        var nextCalls = 0;

        await middleware.InvokeAsync(first, (_, _) => { nextCalls++; return Task.CompletedTask; }, TestContext.Current.CancellationToken);
        await middleware.InvokeAsync(second, (_, _) => { nextCalls++; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        Assert.Equal(2, nextCalls);
        Assert.Equal(2, inbox.MarkProcessedCalls.Count);
    }

    [Fact]
    public async Task UsesProvidedConsumerName()
    {
        var inbox = new FakeInbox();
        var middleware = new InboxDeduplicationMiddleware(inbox, SubId, "custom-consumer");
        var @event = NewEvent();

        await middleware.InvokeAsync(@event, (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken);

        Assert.All(inbox.HasProcessedCalls, c => Assert.Equal("custom-consumer", c.Consumer));
        Assert.All(inbox.MarkProcessedCalls, c => Assert.Equal("custom-consumer", c.Consumer));
    }

    [Fact]
    public void ConstructorValidatesArguments()
    {
        var inbox = new FakeInbox();
        Assert.Throws<ArgumentNullException>(() => new InboxDeduplicationMiddleware(null!, SubId, ConsumerName));
        Assert.Throws<ArgumentException>(() => new InboxDeduplicationMiddleware(inbox, SubId, string.Empty));
    }
}
