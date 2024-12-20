﻿using Azure.Data.Tables;
using Streamon.Subscription;
using System.Runtime.CompilerServices;

namespace Streamon.Azure.TableStorage.Subscription;

public class TableSubscriptionStreamReader(TableClient tableClient, TableStreamStoreOptions options) : ISubscriptionStreamReader
{
    private readonly List<StreamEntity> _streamEntities = [];

    public async IAsyncEnumerable<Event> FetchAsync(StreamPosition fromPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string minEventEntityRowKey = fromPosition.ToEventEntityRowKey(options), maxEventEntityRowKey = StreamPosition.End.ToEventEntityRowKey(options);
        await foreach (var eventEntity in tableClient.QueryAsync<EventEntity>(e => string.Compare(e.RowKey, minEventEntityRowKey) >= 0 && string.Compare(e.RowKey, maxEventEntityRowKey) <= 0 && e.GlobalSequence >= fromPosition.Value, cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            var streamEntity = await GetStreamEntityAsync(StreamId.From(eventEntity.PartitionKey), cancellationToken);
            if (streamEntity.IsDeleted) continue;
            yield return eventEntity.ToEvent(options.StreamTypeProvider);
        }
    }

    public async Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default)
    {
        long globalPosition = 0;
        await foreach (var page in tableClient.QueryAsync<StreamEntity>(e => e.RowKey == options.StreamEntityRowKey, cancellationToken: cancellationToken).AsPages())
            globalPosition = Math.Max(page.Values.Select(e => e.GlobalSequence).DefaultIfEmpty(globalPosition).Max(), globalPosition);
        return StreamPosition.From(globalPosition);
    }

    private async Task<StreamEntity> GetStreamEntityAsync(StreamId streamId, CancellationToken cancellationToken)
    {
        var stream = _streamEntities.SingleOrDefault(e => e.PartitionKey == streamId.Value);
        if (stream is null)
        {
            var response = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, options.StreamEntityRowKey, cancellationToken: cancellationToken);
            if (!response.HasValue) throw new StreamNotFoundException(streamId);
            _streamEntities.Add(stream = response.Value!);
        }
        return stream;
    }
}
