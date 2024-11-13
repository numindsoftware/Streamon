using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class EventIdEntity : ITableEntity
{
    public const string EventIdRowKeyFormat = "SO-ID-{0}";

    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public required long Sequence { get; set; }
}
