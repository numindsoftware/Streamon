using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class StreamEntity : ITableEntity
{
    public const string StreamRowKey = "SO-STREAM";

    /// <summary>
    /// StreamId
    /// </summary>
    public required string PartitionKey { get; set; }
    /// <summary>
    /// Fixed to SO-STREAM value, used to identify the stream entity which helps with concurrency control
    /// </summary>
    public string RowKey { get; set; } = StreamRowKey;
    /// <summary>
    /// Managed by Azure Table Storage, usually indicates the last time the row was accessed or modified
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
    /// <summary>
    /// The ETag value, used for concurrency control by Azure Table Storage
    /// </summary>
    public ETag ETag { get; set; }
    /// <summary>
    /// The last event sequence number, updated on each event append
    /// </summary>
    public long CurrentSequence { get; set; }
    /// <summary>
    /// The global sequence number, updated on each event append for all streams.
    /// In order to calculate the global sequence number, just before saving we query the very last stream and add the new event count.
    /// </summary>
    public long GlobalSequence { get; set; }
    /// <summary>
    /// The date and time when the stream was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
    /// <summary>
    /// The date and time when the stream was last updated
    /// </summary>
    public DateTimeOffset UpdatedOn { get; set; }
    /// <summary>
    /// Soft delete flag, only available at the Stream level for performance reasons
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// Deletion audit information
    /// </summary>
    public DateTimeOffset? DeletedOn { get; set; }
}
