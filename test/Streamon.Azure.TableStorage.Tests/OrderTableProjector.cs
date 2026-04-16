using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

public class OrderTableProjector :
    IEventInitialProjector<OrderCaptured, OrderProjectionEntity>,
    IEventProjector<OrderShipped, OrderProjectionEntity>,
    IEventProjector<OrderCancelled, OrderProjectionEntity>
{
    public OrderProjectionEntity Project(EventHandlerContext<OrderCaptured> @event, CancellationToken cancellationToken = default) =>
        new()
        {
            OrderId = @event.Payload.Id,
            Product = @event.Payload.Product,
            Price = (double)@event.Payload.Price
        };

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderShipped> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderShipped> @event, CancellationToken cancellationToken = default)
    {
        state.Tracking = @event.Payload.Tracking;
        return ValueTask.FromResult(state);
    }

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderCancelled> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderCancelled> @event, CancellationToken cancellationToken = default)
    {
        state.IsCancelled = true;
        return ValueTask.FromResult(state);
    }
}