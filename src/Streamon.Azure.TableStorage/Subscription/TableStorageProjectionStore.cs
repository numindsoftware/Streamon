using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

/// <summary>
/// An <see cref="IProjectionStore{TState}"/> backed by Azure Table Storage. Partition key and row key
/// are derived from <typeparamref name="TState"/> domain properties via the configured selectors,
/// keeping key derivation explicit at registration time.
/// </summary>
/// <typeparam name="TState">The table entity type representing the projection state.</typeparam>
public class TableStorageProjectionStore<TState>(
    TableClient tableClient,
    Func<TState, string> partitionKeySelector,
    Func<TState, string> rowKeySelector) : IProjectionStore<TState>
    where TState : class, ITableEntity, new()
{
    /// <inheritdoc/>
    public async Task<TState?> ReadAsync(TState keyState, CancellationToken cancellationToken = default)
    {
        var pk = partitionKeySelector(keyState);
        var rk = rowKeySelector(keyState);
        var response = await tableClient.GetEntityIfExistsAsync<TState>(pk, rk, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.HasValue ? response.Value : default;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(TState state, CancellationToken cancellationToken = default)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        state.PartitionKey = partitionKeySelector(state);
        state.RowKey = rowKeySelector(state);
        await tableClient.UpsertEntityAsync(state, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }
}