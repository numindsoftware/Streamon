using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

public class OrderInMemoryProjector : IEventAsyncHandler
{
    public static Dictionary<StreamId, OrderProjection> Projections { get; } = [];

    public ValueTask HandleAsync(EventConsumeContext<object> context, CancellationToken cancellationToken = default)
    {
        Projections.TryGetValue(context.StreamId, out var projection);
        Projections[context.StreamId] = context.Payload switch
        {
            OrderCaptured e => new OrderProjection(e.Id, new(e.Product, e.Price), context.Timestamp),
            OrderCancelled => projection! with { CancelledOn = context.Timestamp },
            _ => projection!
        };
        return ValueTask.CompletedTask;
    }
}
