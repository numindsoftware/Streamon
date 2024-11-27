using Streamon.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Subscription.Tests;

internal class TestEventHandler : EventHandler
{
    public TestEventHandler()
    {
        On<OrderCaptured>((context, cancellationToken) =>
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
    private readonly Dictionary<SubscriptionId, StreamPosition> _checkpoints = [];

    public Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_checkpoints.TryGetValue(subscriptionId, out var position) ? position : StreamPosition.Start);

    public Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default) =>
        Task.FromResult(_checkpoints[subscriptionId] = position);
}

internal class SubscriptionStreamReader : ISubscriptionStreamReader
{
    private readonly List<Event> _positions = [];

    public async IAsyncEnumerable<Event> FetchAsync(StreamPosition position, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in _positions)
        {
            if (item.StreamPosition > position)
            {
                yield return await Task.FromResult(item);
            }
        }
    }

    public Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default) => Task.FromResult(StreamPosition.Start);
}