using Azure.Data.Tables;
using Streamon.Subscription;
using System.Runtime.CompilerServices;

namespace Streamon.Azure.TableStorage.Subscription;

internal class TableSubscriptionStreamReader(TableClient tableClient, TableStreamStoreOptions options) : ISubscriptionStreamReader
{
    public async IAsyncEnumerable<Event> FetchAsync(Checkpoint fromCheckpoint, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var eventEntity in tableClient.QueryAsync<EventEntity>(e => e.RowKey != options.StreamEntityRowKey && e.GlobalSequence >= fromCheckpoint.Position, cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            //if (eventEntity.RowKey == options.StreamEntityRowKey) 
            //yield return eventEntity.ToEventEnvelope(streamTypeProvider);
        }

        //await foreach (var streamEntity in tableClient.QueryAsync<StreamEntity>(e => e.GlobalSequence >= fromCheckpoint.Position, cancellationToken: cancellationToken))
        //{
        //    List<EventEnvelope> events = [];
        //    await foreach (var eventEntity in tableClient.QueryAsync<EventEntity>(e => e.PartitionKey == streamEntity.PartitionKey && e.GlobalSequence >= fromCheckpoint.Position, cancellationToken: cancellationToken))
        //    {
        //        events.Add(eventEntity.ToEventEnvelope(streamTypeProvider));
        //    }
        //    if (cancellationToken.IsCancellationRequested) yield break;
        //    yield return new(StreamId.From(streamEntity.PartitionKey), StreamPosition.From(streamEntity.GlobalSequence), events);
        //}
    }
}
