using Azure.Data.Tables;
using Streamon.Subscription;
using System.Runtime.CompilerServices;

namespace Streamon.Azure.TableStorage.Subscription;

public class TableSubscriptionStreamReader(TableClient tableClient, TableStreamStoreOptions options) : ISubscriptionStreamReader
{
    public async IAsyncEnumerable<Event> FetchAsync(StreamPosition fromPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string minRowKey = fromPosition.ToGlobalEventIndexRowKey();
        await foreach (var indexEntity in tableClient.QueryAsync<GlobalEventIndexEntity>(
            e => e.PartitionKey == options.GlobalEventIndexPartitionKey && string.Compare(e.RowKey, minRowKey) >= 0,
            cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            if (indexEntity.IsDeleted) continue;
            yield return indexEntity.ToEvent(options.StreamTypeProvider);
        }
    }

    public async Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default)
    {
        var response = await tableClient.GetEntityIfExistsAsync<GlobalPositionEntity>(
            options.GlobalPartitionKey, options.GlobalMetaRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.HasValue
            ? StreamPosition.From(response.Value!.GlobalSequence)
            : StreamPosition.Start;
    }
}
