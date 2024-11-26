using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage.Subscription;

public class CheckpointEntity : ITableEntity
{
    /// <summary>
    /// Stream table name
    /// </summary>
    public required string PartitionKey { get; set; }
    /// <summary>
    /// SubscriptionId
    /// </summary>
    public required string RowKey { get; set; }
    /// <summary>
    /// Subscription Position
    /// </summary>
    public required long Position { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
