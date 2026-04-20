using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

/// <summary>
/// Fat denormalized index entity stored in the GEVT partition.
/// One row per event, RowKey is the zero-padded global position for natural sort order.
/// </summary>
internal class GlobalEventIndexEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public required string StreamId { get; set; }
    public required long Sequence { get; set; }
    public required long GlobalSequence { get; set; }
    public required string EventId { get; set; }
    public required string BatchId { get; set; }
    public required string Type { get; set; }
    public required string Data { get; set; }
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
}