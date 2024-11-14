using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

public class TableStreamStore(TableClient tableClient, IStreamTypeProvider streamTypeProvider, TableStreamStoreOptions? options = default) : IStreamStore
{
    private readonly TableStreamStoreOptions _options = options ?? new();

    public async Task<Stream> FetchAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        endPosition = endPosition == default ? StreamPosition.End : endPosition;
        
        List<TableEntity> entities = [];
        await foreach(var entity in tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == streamId.Value && !e.RowKey.StartsWith(EventIdEntity.EventIdRowKeyPrefix), cancellationToken: cancellationToken)) entities.Add(entity);
        
        if (entities.Count == 0) throw new StreamNotFoundException(streamId);
        var streamEntity = entities.ExtractStreamEntity();
        var eventEnvelopes = entities.ToEventEnvelopes(streamTypeProvider);

        return new Stream(streamId, [.. eventEnvelopes], new(streamEntity.CurrentSequence));
    }

    public async Task<Stream> AppendAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var streamResult = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, StreamEntity.StreamRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (streamResult.Value is not null && streamResult.Value.IsDeleted) throw new StreamDeletedException(streamId, $"The Stream {streamId} has been deleted.");
        var streamEntity = streamResult.Value ?? streamId.ToStreamEntity(StreamPosition.Start, StreamPosition.Start);
        
        StreamPosition currentPosition = new(streamEntity.CurrentSequence);
        if (currentPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, currentPosition);

        var envelopes = events.Select(@event =>
        {
            currentPosition = currentPosition.Next();
            return @event.ToEventEnvelope(currentPosition, DateTimeOffset.UtcNow, metadata);
        }).ToArray();
        var eventPairs = envelopes.SelectMany(envelope => envelope.ToEventEntityPair(streamId, metadata, streamTypeProvider));

        // global position can be useful when projecting to event streams
        streamEntity.CurrentSequence = currentPosition.Value;
        streamEntity.GlobalSequence = _options.CalculateGlobalPosition
                ? (await FetchLatestGlobalPositionAsync(cancellationToken).ConfigureAwait(false)).Value 
                : currentPosition.Value;


        //await tableClient.UpsertEntityAsync(streamEntity, TableUpdateMode.Merge, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new Stream(streamId, envelopes, currentPosition);
    }

    public async Task DeleteStreamAsync(StreamId streamId, StreamPosition expectedSequence, CancellationToken cancellationToken = default)
    {
        var streamResult = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, StreamEntity.StreamRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!streamResult.HasValue) throw new StreamNotFoundException(streamId);
        var streamEntity = streamResult.Value!;
        if (_options.DisableSoftDelete)
        {
            var deleteResponse = await tableClient.DeleteEntityAsync(streamEntity.PartitionKey, streamEntity.RowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            deleteResponse.ThrowOnError($"Failed to delete stream with status code {deleteResponse.Status}");
        }
        else
        {
            streamEntity.IsDeleted = true;
            streamEntity.DeletedOn = DateTimeOffset.Now;
            var deleteResponse = await tableClient.UpdateEntityAsync(streamEntity, streamEntity.ETag, cancellationToken: cancellationToken).ConfigureAwait(false);
            deleteResponse.ThrowOnError($"Failed to soft delete stream with status code {deleteResponse.Status}");
        }
    }

    private async Task<StreamPosition> FetchLatestGlobalPositionAsync(CancellationToken cancellationToken = default)
    {
        List<StreamEntity> entities = [];
        await foreach (var entity in tableClient.QueryAsync<StreamEntity>(e => e.RowKey == StreamEntity.StreamRowKey, cancellationToken: cancellationToken)) entities.Add(entity);
        return entities.Count == 0 ? StreamPosition.Start : new(entities.Sum(e => e.CurrentSequence));
    }
}
