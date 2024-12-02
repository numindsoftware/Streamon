
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public class TableCheckpointStore(TableClient checkpointTableClient, string streamTableName) : ICheckpointStore
{
    public const string DefaultCheckpointTableName = "StreamonCheckpoint";

    public async Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        var response = await checkpointTableClient.GetEntityIfExistsAsync<CheckpointEntity>(streamTableName, subscriptionId.Value, cancellationToken: cancellationToken);
        return response.HasValue ? StreamPosition.From(response.Value!.Position) : StreamPosition.End;
    }

    public async Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default)
    {
        await checkpointTableClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var response = await checkpointTableClient.UpsertEntityAsync(new CheckpointEntity
        {
            PartitionKey = streamTableName,
            RowKey = subscriptionId.ToString(),
            Position = position.Value
        }, cancellationToken: cancellationToken);
        response.ThrowOnError();
    }
}
