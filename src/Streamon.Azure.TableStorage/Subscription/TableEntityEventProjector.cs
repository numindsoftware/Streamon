using Azure;
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public abstract class TableEntityEventProjector<TEntity>(TableClient tableClient) : IEventProjector where TEntity : class, ITableEntity, new()
{
    protected abstract Task HandleAsync(TEntity entity, Event @event, CancellationToken cancellationToken = default);

    protected virtual string GetPartitionKey(Event @event) => @event.StreamId.Value;

    protected abstract string GetRowKey(Event @event);

    public async Task ProjectAsync(Event @event, CancellationToken cancellationToken = default)
    {
        var partitionKey = GetPartitionKey(@event);
        var rowKey = GetRowKey(@event);
        await tableClient.CreateIfNotExistsAsync(cancellationToken);
        var entityResult = await tableClient.GetEntityAsync<TEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
        if (entityResult is { HasValue: true, Value: var entity }) // entity already exists
        {
            await HandleAsync(entity, @event, cancellationToken);
            await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
        }
        else
        {
            entity = new()
            {
                PartitionKey = partitionKey, // Set the partition key as needed
                RowKey = rowKey, // Generate a new unique row key
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All, // Set the ETag to All for optimistic concurrency
            };
            await HandleAsync(entity, @event, cancellationToken);
            await tableClient.CreateIfNotExistsAsync(cancellationToken);
            await tableClient.AddEntityAsync(entity, cancellationToken);
        }
    }
}
