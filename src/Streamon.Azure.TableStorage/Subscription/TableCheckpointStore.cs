using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public class TableCheckpointStore(TableServiceClient checkpointTableServiceClient, string tableName) : ICheckpointStore
{
    public async Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default)
    {
        if (!await checkpointTableServiceClient.CheckTableExistsAsync(tableName, cancellationToken)) return StreamPosition.End;
        var checkpointTableClient = checkpointTableServiceClient.GetTableClient(tableName);
        var response = await checkpointTableClient.GetEntityIfExistsAsync<CheckpointEntity>(string.Empty, subscriptionId.Value, cancellationToken: cancellationToken);
        return response.HasValue ? StreamPosition.From(response.Value!.Position) : StreamPosition.End;
    }

    public async Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default)
    {
        var checkpointTableClient = checkpointTableServiceClient.GetTableClient(tableName);
        await checkpointTableClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var response = await checkpointTableClient.UpsertEntityAsync(new CheckpointEntity
        {
            PartitionKey = string.Empty,
            RowKey = subscriptionId.ToString(),
            Position = position.Value
        }, cancellationToken: cancellationToken);
        response.ThrowOnError();
    }
}
