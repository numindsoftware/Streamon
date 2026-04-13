using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

public class OrderInMemoryProjector : IEventHandler
{
    public static Dictionary<StreamId, OrderProjection> Projections { get; } = [];

    public Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        Projections.TryGetValue(@event.StreamId, out var projection);
        Projections[@event.StreamId] = @event.Payload switch
        {
            OrderCaptured e => new OrderProjection(e.Id, new(e.Product, e.Price), @event.Timestamp),
            OrderCancelled => projection! with { CancelledOn = @event.Timestamp },
            _ => projection!
        };
        return Task.CompletedTask;
    }
}
