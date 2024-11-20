using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class EventEntity : ITableEntity
{
    /// <summary>
    /// Stream Identifier
    /// </summary>
    public required string PartitionKey { get; set; }
    /// <summary>
    /// SS-EVENT-{sequence}: Event line
    /// </summary>
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public required long Sequence { get; set; }
    public required long GlobalSequence { get; set; }
    /// <summary>
    /// An id assigned to a group of events that are appended together
    /// </summary>
    public required string BatchId { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
    /// <summary>
    /// Fully qualified type name
    /// </summary>
    public required string Type { get; set; }
    /// <summary>
    /// The unique identifier of the event
    /// </summary>
    public required string EventId { get; set; }
    public required string Data { get; set; }
    public string? Metadata { get; set; }
}
