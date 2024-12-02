
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public class TableCheckpointStore(TableClient checkpointTableClient, string? streamTableName = default) : ICheckpointStore
{
    private readonly string _streamTableName = streamTableName ?? "StreamonCheckpoint";

    public async Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        var response = await checkpointTableClient.GetEntityIfExistsAsync<CheckpointEntity>(_streamTableName, subscriptionId.Value, cancellationToken: cancellationToken);
        if (!response.HasValue) throw new CheckpointNotFoundException(subscriptionId);
        return StreamPosition.From(response.Value!.Position);
    }

    public async Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default)
    {
        var response = await checkpointTableClient.UpsertEntityAsync(new CheckpointEntity
        {
            PartitionKey = _streamTableName,
            RowKey = subscriptionId.ToString(),
            Position = position.Value
        }, cancellationToken: cancellationToken);
        response.ThrowOnError();
    }
}
