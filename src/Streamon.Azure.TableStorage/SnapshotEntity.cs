using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class SnapshotEntity : ITableEntity
{
    public const string SnapshotRowKeyPrefix = "SO-SNAP-";
    public const string EventIdRowKeyFormat = SnapshotRowKeyPrefix + "{0}";

    /// <summary>
    /// The stream identifier
    /// </summary>
    public required string PartitionKey { get; set; }
    /// <summary>
    /// The snapshot identifier, only one snapshot per type per stream
    /// </summary>
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    /// <summary>
    /// The event sequence number when the snapshot was taken
    /// </summary>
    public required long Sequence { get; set; }
    /// <summary>
    /// The global sequence number when the snapshot was taken
    /// </summary>
    public required long GlobalSequence { get; set; }
    /// <summary>
    /// The date and time when the snapshot was created
    /// </summary>
    public required string CreatedOn { get; set; }
    /// <summary>
    /// The type marker to deserialize the snapshot data, only one per type per stream
    /// </summary>
    public required string Type { get; set; }
    /// <summary>
    /// The snapshot json serialized data
    /// </summary>
    public required string Data { get; set; }
}
