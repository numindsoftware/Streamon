using Azure;
using Azure.Data.Tables;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.TableStorage.Tests;

public class OrderProjectionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string OrderId { get; set; } = default!;
    public string? Tracking { get; set; }
    public bool IsCancelled { get; set; }
    public OrderAddress? ShippingAddress { get; set; }
    public List<OrderItem>? Items { get; set; }
}
