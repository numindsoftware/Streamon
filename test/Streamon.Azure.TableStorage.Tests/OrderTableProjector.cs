using Streamon.Subscription;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

public class OrderTableProjector :
    IEventInitialProjector<OrderCaptured, OrderProjectionEntity>,
    IEventProjector<OrderShipped, OrderProjectionEntity>,
    IEventProjector<OrderCancelled, OrderProjectionEntity>,
    IEventProjector<OrderDetailsUpdated, OrderProjectionEntity>,
    IEventProjector<OrderAddressChanged, OrderProjectionEntity>,
    IEventProjector<OrderItemsAdded, OrderProjectionEntity>,
    IEventProjector<OrderItemsCancelled, OrderProjectionEntity>,
    IEventProjector<OrderItemsReplaced, OrderProjectionEntity>
{
    public OrderProjectionEntity Project(EventHandlerContext<OrderCaptured> @event, CancellationToken cancellationToken = default) =>
        new()
        {
            OrderId = @event.Payload.Id,
            ShippingAddress = @event.Payload.OrderAddress,
            Items = [.. @event.Payload.Items]
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

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderDetailsUpdated> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderDetailsUpdated> @event, CancellationToken cancellationToken = default)
    {
        state.ShippingAddress = @event.Payload.ShippingAddress;
        state.Items = @event.Payload.Items;
        return ValueTask.FromResult(state);
    }

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderAddressChanged> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderAddressChanged> @event, CancellationToken cancellationToken = default)
    {
        state.ShippingAddress = @event.Payload.ShippingAddress;
        return ValueTask.FromResult(state);
    }

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderItemsAdded> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderItemsAdded> @event, CancellationToken cancellationToken = default)
    {
        state.Items ??= [];
        state.Items.AddRange(@event.Payload.Items);
        return ValueTask.FromResult(state);
    }

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderItemsCancelled> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderItemsCancelled> @event, CancellationToken cancellationToken = default)
    {
        state.Items?.RemoveAll(i => @event.Payload.ItemNames.Contains(i.Name));
        return ValueTask.FromResult(state);
    }

    public OrderProjectionEntity GetKey(EventHandlerContext<OrderItemsReplaced> @event, CancellationToken cancellationToken = default) =>
        new() { OrderId = @event.Payload.Id };

    public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventHandlerContext<OrderItemsReplaced> @event, CancellationToken cancellationToken = default)
    {
        state.Items = @event.Payload.Items;
        return ValueTask.FromResult(state);
    }
}