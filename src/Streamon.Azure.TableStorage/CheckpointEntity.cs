using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

public class CheckpointEntity : ITableEntity
{
    /// <summary>
    /// SubscriptionId
    /// </summary>
    public required string PartitionKey { get; set; }
    /// <summary>
    /// Subscription Position
    /// </summary>
    public required string RowKey { get; set; }
    public required DateTimeOffset UpdatedOn { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public void SetSubscriptionPosition(StreamPosition position)
    {
        RowKey = position.Value.ToString();
        UpdatedOn = DateTimeOffset.Now;
    }

    public StreamPosition GetSubscriptionPosition() => 
        StreamPosition.From(long.Parse(RowKey));
}
