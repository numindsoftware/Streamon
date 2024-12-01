using Streamon.Tests.Fixtures;
using System.Runtime.CompilerServices;

namespace Streamon.Subscription.Tests;

internal class TestEventHandler : EventHandler
{
    public TestEventHandler()
    {
        On<OrderShipped>((context, cancellationToken) =>
        {
            Assert.Equal(StreamId.From("order-123"), context.StreamId);

            return ValueTask.CompletedTask;
        });
    }
}

internal class EventHandlerResolver : IEventHandlerResolver
{
    public IEventHandler Resolve(Type eventType) => (IEventHandler)Activator.CreateInstance(eventType)!;
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
            if (item.StreamPosition > position)
            {
                yield return await Task.FromResult(item);
            }
        }
    }

    public Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default) => Task.FromResult(StreamPosition.Start);
}