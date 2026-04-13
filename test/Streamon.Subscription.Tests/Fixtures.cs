using Streamon.Tests.Fixtures;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Streamon.Subscription.Tests;

internal class TestEventHandler : IEventHandler<OrderShipped>
{
    public Task HandleAsync(EventHandlerContext<OrderShipped> context, CancellationToken cancellationToken = default)
    {
        Assert.Equal(StreamId.From("order-123"), context.StreamId);
        return Task.CompletedTask;
    }
}

internal class TrackingHandler : IEventHandler
{
    public List<Event> HandledEvents { get; } = [];

    public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        HandledEvents.Add(@event);
        return Task.CompletedTask;
    }
}

internal class ConcurrentTrackingHandler : IEventHandler
{
    public ConcurrentBag<(Event Event, int ManagedThreadId)> HandledEvents { get; } = [];

    public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        HandledEvents.Add((@event, Environment.CurrentManagedThreadId));
        return Task.CompletedTask;
    }
}

internal class FailingHandler(int failOnNth = 1) : IEventHandler
{
    private int _callCount;

    public int CallCount => _callCount;

    public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _callCount) == failOnNth)
        {
            throw new InvalidOperationException("Simulated handler failure");
        }
        return Task.CompletedTask;
    }
}

internal class TrackingMiddleware : IEventMiddleware
{
    public List<Event> BeforeEvents { get; } = [];
    public List<Event> AfterEvents { get; } = [];

    public async Task InvokeAsync(Event context, EventHandlerDelegate next, CancellationToken cancellationToken)
    {
        BeforeEvents.Add(context);
        await next(context, cancellationToken).ConfigureAwait(false);
        AfterEvents.Add(context);
    }
}

internal class ShortCircuitMiddleware : IEventMiddleware
{
    public List<Event> InterceptedEvents { get; } = [];

    public Task InvokeAsync(Event context, EventHandlerDelegate next, CancellationToken cancellationToken)
    {
        InterceptedEvents.Add(context);
        return Task.CompletedTask; // never calls next
    }
}

internal class ConcurrentDispatchMiddleware : IEventMiddleware
{
    public async Task InvokeAsync(Event context, EventHandlerDelegate next, CancellationToken cancellationToken)
    {
        await Task.Run(() => next(context, cancellationToken).ConfigureAwait(false));
    }
}

internal class CheckpointStore : ICheckpointStore
{
    public Dictionary<SubscriptionId, StreamPosition> Checkpoints { get; }= [];

    public Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Checkpoints.TryGetValue(subscriptionId, out var position) ? position : StreamPosition.End);

    public Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default) =>
        Task.FromResult(Checkpoints[subscriptionId] = position);
}

internal class SubscriptionStreamReader : ISubscriptionStreamReader
{
    public List<Event> Events { get; } = [];

    public async IAsyncEnumerable<Event> FetchAsync(StreamPosition position, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in Events)
        {
            if (item.GlobalPosition > position)
            {
                yield return await Task.FromResult(item);
            }
        }
    }

    public Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Events.Count > 0 ? Events[^1].GlobalPosition : StreamPosition.Start);
}