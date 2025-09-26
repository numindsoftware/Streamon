using Azure;
using Azure.Data.Tables;
using Streamon;
using Streamon.Azure.TableStorage.Subscription;
using Streamon.Subscription;
using System.Text.Json;

namespace OrderProcessing;

internal class OrderProjector(TableClient tableClient, JsonSerializerOptions jsonSerializerOptions) : TableEntityEventProjector<OrderProjectionEntity>(tableClient),
    //IEventAsyncInitialProjector<OrderPlaced, OrderProjectionEntity>,
    //IEventAsyncProjector<OrderCancelled, OrderProjectionEntity>,
    //IEventAsyncProjector<OrderShipped, OrderProjectionEntity>,
    //IEventAsyncProjector<OrderAccepted, OrderProjectionEntity>,
    //IEventAsyncProjector<OrderCompleted, OrderProjectionEntity>
    IEventAsyncProjector<OrderPlaced>,
    IEventAsyncProjector<OrderCancelled>,
    IEventAsyncProjector<OrderShipped>,
    IEventAsyncProjector<OrderAccepted>,
    IEventAsyncProjector<OrderCompleted>
{
    //public string GetIdentity(EventConsumeContext<OrderPlaced> @event, CancellationToken cancellationToken = default) => @event.Payload.OrderId;
    //public string GetIdentity(EventConsumeContext<OrderCancelled> @event, CancellationToken cancellationToken = default) => @event.Payload.OrderId;
    //public string GetIdentity(EventConsumeContext<OrderShipped> @event, CancellationToken cancellationToken = default) => @event.Payload.OrderId;
    //public string GetIdentity(EventConsumeContext<OrderAccepted> @event, CancellationToken cancellationToken = default) => @event.Payload.OrderId;
    //public string GetIdentity(EventConsumeContext<OrderCompleted> @event, CancellationToken cancellationToken = default) => @event.Payload.OrderId;

    //public ValueTask<OrderProjectionEntity> ProjectAsync(EventConsumeContext<OrderPlaced> @event, CancellationToken cancellationToken = default) =>
    //    ValueTask.FromResult<OrderProjectionEntity>(new()
    //    {
    //        PartitionKey = @event.StreamId.Value,
    //        RowKey = @event.Payload.OrderId,
    //        CustomerName = @event.Payload.CustomerName,
    //        CustomerOrderNumber = @event.Payload.CustomerOrderNumber,
    //        ShippingAddress = @event.Payload.ShippingAddress,
    //        OrderStatus = "Placed",
    //        OrderItems = JsonSerializer.Serialize(@event.Payload.Items, jsonSerializerOptions),
    //        OrderDate = DateTimeOffset.UtcNow
    //    });

    //public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventConsumeContext<OrderCancelled> @event, CancellationToken cancellationToken = default)
    //{
    //    state.OrderStatus = "Cancelled";
    //    return ValueTask.FromResult(state);
    //}

    //public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventConsumeContext<OrderShipped> @event, CancellationToken cancellationToken = default)
    //{
    //    state.OrderStatus = "Shipped";
    //    return ValueTask.FromResult(state);
    //}

    //public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventConsumeContext<OrderAccepted> @event, CancellationToken cancellationToken = default)
    //{
    //    state.OrderStatus = "Accepted";
    //    return ValueTask.FromResult(state);
    //}

    //public ValueTask<OrderProjectionEntity> ProjectAsync(OrderProjectionEntity state, EventConsumeContext<OrderCompleted> @event, CancellationToken cancellationToken = default)
    //{
    //    state.OrderStatus = "Completed";
    //    return ValueTask.FromResult(state);
    //}

    protected override string GetRowKey(Event @event) =>
        @event.Payload switch
        {
            OrderPlaced orderPlaced => orderPlaced.OrderId,
            OrderCancelled orderCancelled => orderCancelled.OrderId,
            OrderShipped orderShipped => orderShipped.OrderId,
            OrderAccepted orderAccepted => orderAccepted.OrderId,
            OrderCompleted orderCompleted => orderCompleted.OrderId,
            _ => throw new NotImplementedException($"Event type {@event.Payload.GetType()} is not supported.")
        };

    protected override Task HandleAsync(OrderProjectionEntity entity, Event @event, CancellationToken cancellationToken = default)
    {
        switch (@event.Payload)
        {
            case OrderPlaced orderPlaced:
                entity.CustomerName = orderPlaced.CustomerName;
                entity.CustomerOrderNumber = orderPlaced.CustomerOrderNumber;
                entity.ShippingAddress = orderPlaced.ShippingAddress;
                entity.OrderStatus = "Placed";
                entity.OrderItems = JsonSerializer.Serialize(orderPlaced.Items, jsonSerializerOptions);
                entity.OrderDate = DateTimeOffset.UtcNow;
                break;
            case OrderCancelled orderCancelled:
                entity.OrderStatus = "Cancelled";
                break;
            case OrderShipped orderShipped:
                entity.OrderStatus = "Shipped";
                break;
            case OrderAccepted orderAccepted:
                entity.OrderStatus = "Accepted";
                break;
            case OrderCompleted orderCompleted:
                entity.OrderStatus = "Completed";
                break;
            default:
                throw new NotImplementedException($"Event type {@event.Payload.GetType()} is not supported.");
        };
        return Task.FromResult(entity);
    }
}

public class OrderProjectionEntity : ITableEntity
{
    //streamId
    public string? PartitionKey { get; set; }
    // OrderId
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerOrderNumber { get; set; }
    public string? ShippingAddress { get; set; }
    public string? OrderStatus { get; set; }
    public string? OrderItems { get; set; } // JSON serialized list of OrderItem
    public DateTimeOffset OrderDate { get; set; } // ISO 8601 format
    public decimal OrderTotal { get; set; }
    public string Currency { get; set; } = "USD";
}
