using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage.Subscription;

internal sealed class InboxEntryEntity : ITableEntity
{
    /// <summary>"{subscriptionId}|{consumerName}" — groups all entries for one consumer in a partition.</summary>
    public string PartitionKey { get; set; } = default!;

    /// <summary>The processed <see cref="EventId"/> value (ULID, lexicographically sortable).</summary>
    public string RowKey { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long GlobalPosition { get; set; }
    public string StreamId { get; set; } = default!;
    public DateTimeOffset ProcessedOn { get; set; }
}