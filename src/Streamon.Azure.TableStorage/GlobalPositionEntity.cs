using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

/// <summary>
/// Singleton entity that tracks the latest global sequence number across all streams.
/// Stored at PartitionKey="__GLOBAL__", RowKey="SO-META". ETag is used for optimistic concurrency.
/// </summary>
internal class GlobalPositionEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    /// <summary>
    /// The latest global sequence number allocated across all streams.
    /// </summary>
    public long GlobalSequence { get; set; }
    public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.UtcNow;
}