using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class EventEntity : ITableEntity
{
    public const string EventRowKeyPrefix = "SO-EVENT-";
    public const string EventRowKeyFormat = EventRowKeyPrefix + "{0:000000000000000000}";

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
