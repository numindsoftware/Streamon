using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage.Tests;

public class OrderProjectionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string OrderId { get; set; } = default!;
    public string Product { get; set; } = default!;
    public double Price { get; set; }
    public string? Tracking { get; set; }
    public bool IsCancelled { get; set; }
}