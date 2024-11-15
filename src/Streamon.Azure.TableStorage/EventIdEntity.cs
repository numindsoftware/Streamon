using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class EventIdEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
