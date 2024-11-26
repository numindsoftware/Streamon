
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

internal class TableCheckpointStore(string streamTableName, TableClient checkpointTableClient) : ICheckpointStore
{
    public async Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        var response = await checkpointTableClient.GetEntityIfExistsAsync<CheckpointEntity>(streamTableName, subscriptionId.Value, cancellationToken: cancellationToken);
        if (!response.HasValue) throw new CheckpointNotFoundException(subscriptionId);
        return StreamPosition.From(response.Value!.Position);
    }

    public async Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default)
    {
        var response = await checkpointTableClient.UpsertEntityAsync(new CheckpointEntity
        {
            PartitionKey = streamTableName,
            RowKey = subscriptionId.ToString(),
            Position = position.Value
        }, cancellationToken: cancellationToken);
        response.ThrowOnError();
    }
}
