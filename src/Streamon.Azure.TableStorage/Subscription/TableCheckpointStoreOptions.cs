namespace Streamon.Azure.TableStorage.Subscription;

public class TableCheckpointStoreOptions
{
    public string TableName { get; set; } = "StreamonSubscriptionCheckpoint";
}