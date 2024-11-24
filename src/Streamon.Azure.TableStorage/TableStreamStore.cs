using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace Streamon.Azure.TableStorage;

public class TableStreamStore(TableClient tableClient, TableStreamStoreOptions options) : IStreamStore
{
    public event EventHandler<StreamEventArgs>? EventsAppended;
    public event EventHandler<StreamIdEventArgs>? StreamDeleted;

    public async Task<Stream> FetchAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        endPosition = endPosition == default ? StreamPosition.End : endPosition;
        
        var streamEntityResponse = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, options.StreamEntityRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!streamEntityResponse.HasValue) throw new StreamNotFoundException(streamId);

        List<EventEntity> entities = [];
        // using plain odata filtering to only get event entities, this odata implementation does not support 'startsWith' operators so we use a range query trick as explained in:
        // https://learn.microsoft.com/en-us/rest/api/storageservices/querying-tables-and-entities
        var eventsQuery = $"PartitionKey eq '{streamId.Value}' and RowKey ge '{options.EventEntityRowKeyPrefix}0' and RowKey le '{options.EventEntityRowKeyPrefix}9' and Sequence ge {startPosition} and Sequence le {endPosition}";
        await foreach (var entity in tableClient.QueryAsync<EventEntity>(eventsQuery, cancellationToken: cancellationToken)) entities.Add(entity);
        if (entities.Count == 0) throw new StreamNotFoundException(streamId);
        
        var eventEnvelopes = entities.ToEventEnvelopes(options);
        return new Stream(streamId, StreamPosition.From(streamEntityResponse.Value!.Sequence), [.. eventEnvelopes]);
    }

    public async Task<Stream> AppendAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        if (expectedPosition == StreamPosition.End) throw new StreamPositionOutOfRangeException(expectedPosition, $"Can't append events past the end position.");
        if (events.Count() * 2 >= options.TransactionBatchSize + (options.TransactionBatchSize % 2) - 2) throw new BatchSizeExceededException(events.Count(), options.TransactionBatchSize, $"The number of events to append exceeds the maximum batch size of {options.TransactionBatchSize}.");

        var streamResult = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, options.StreamEntityRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (streamResult.HasValue && streamResult.Value!.IsDeleted) throw new StreamDeletedException(streamId, $"The Stream {streamId} has been deleted.");
        var streamEntity = streamResult.HasValue ? streamResult.Value! : streamId.ToStreamEntity(StreamPosition.Start, StreamPosition.Start, options);
        
        StreamPosition currentPosition = StreamPosition.From(streamEntity.Sequence);
        if (currentPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, currentPosition);
        var globalPosition = StreamPosition.From(await FetchLatestGlobalPositionAsync(streamEntity.GlobalSequence, cancellationToken).ConfigureAwait(false));

        var batchId = BatchId.New();
        List<TableTransactionAction> streamTransactions = [];
        List<EventEnvelope> eventEnvelopes = [];
        foreach (var @event in events)
        {
            currentPosition = currentPosition.Next();
            globalPosition = globalPosition.Next();
            var eventEnvelope = @event.ToEventEnvelope(streamId, batchId, currentPosition, globalPosition, metadata);
            var eventEntity = @event.ToEventEntity(eventEnvelope.EventId, streamId, batchId, currentPosition, globalPosition, metadata, options.StreamTypeProvider, options);
            EventIdEntity eventIdEntity = new() { PartitionKey = streamId.Value, RowKey = eventEnvelope.EventId.ToEventIdEntityRowKey(options), Sequence = currentPosition.Value };
            streamTransactions.Add(new(TableTransactionActionType.Add, eventEntity));
            streamTransactions.Add(new(TableTransactionActionType.Add, eventIdEntity));
            eventEnvelopes.Add(eventEnvelope);
        }

        streamEntity.Sequence = currentPosition.Value;
        streamEntity.GlobalSequence = globalPosition.Value;
        streamEntity.UpdatedOn = DateTimeOffset.Now;
        streamTransactions.Add(new(TableTransactionActionType.UpsertMerge, streamEntity));
        try
        {
            var response = await tableClient.SubmitTransactionAsync(streamTransactions, cancellationToken).ConfigureAwait(false);
            response.Value.ToList().ForEach(r => r.ThrowOnError($"Failed to upsert stream with status code {r.Status}"));
        }
        catch (TableTransactionFailedException ex) when (ex.ErrorCode == TableErrorCode.EntityAlreadyExists || ex.ErrorCode == TableErrorCode.InvalidDuplicateRow)
        {
            var entityId = new EventId(streamTransactions[ex.FailedTransactionActionIndex!.Value].Entity.RowKey.Replace(options.EventEntityRowKeyPrefix, string.Empty));
            throw new DuplicateEventException(entityId);
        }
        catch (RequestFailedException ex)
        {
            throw new TableStorageOperationException($"An error occurred while saving events. {ex.Message}", ex);
        }
        return new Stream(streamId, globalPosition, eventEnvelopes);
    }

    public async Task<long> DeleteStreamAsync(StreamId streamId, StreamPosition expectedPosition, CancellationToken cancellationToken = default)
    {
        var streamResult = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, options.StreamEntityRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!streamResult.HasValue) throw new StreamNotFoundException(streamId);

        var currentPosition = StreamPosition.From(streamResult.Value!.Sequence);
        if (currentPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, currentPosition);
        var streamEntity = streamResult.Value!;

        if (options.DeleteMode == StreamDeleteMode.Soft)
        {
            streamEntity.IsDeleted = true;
            streamEntity.DeletedOn = DateTimeOffset.Now;
            var deleteResponse = await tableClient.UpdateEntityAsync(streamEntity, streamEntity.ETag, cancellationToken: cancellationToken).ConfigureAwait(false);
            deleteResponse.ThrowOnError($"Failed to soft delete stream with status code {deleteResponse.Status}");
            return streamEntity.Sequence;
        }
        else
        {
            List<ITableEntity> deletableEntities = [];
            await foreach (var deleteEntity in tableClient.QueryAsync<ITableEntity>(e => e.PartitionKey == streamId.Value, cancellationToken: cancellationToken))
            {
                deletableEntities.Add(deleteEntity);
            }
            foreach (var batch in GenerateDeleteBatch(deletableEntities))
            {
                var response = await tableClient.SubmitTransactionAsync(batch, cancellationToken: cancellationToken).ConfigureAwait(false);
                response.Value.ToList().ForEach(r => r.ThrowOnError($"Failed to delete stream entities with status code {r.Status}"));
            }
            return deletableEntities.Count;
        }
    }

    protected virtual void OnEventsAppended(Stream stream)
    {
        options.OnEventsAppended?.Invoke(stream);
        EventsAppended?.Invoke(this, new(stream));
    }
    protected virtual void OnStreamDeleted(StreamId streamId)
    {
        options.OnStreamDeleted?.Invoke(streamId);
        StreamDeleted?.Invoke(this, new(streamId));
    }

    private IEnumerable<IEnumerable<TableTransactionAction>> GenerateDeleteBatch(IEnumerable<ITableEntity> tableEntities)
    {
        List<TableTransactionAction> batch = [];
        foreach (var entity in tableEntities)
        {
            batch.Add(new(TableTransactionActionType.Delete, entity));
            if (batch.Count >= options.TransactionBatchSize)
            {
                yield return batch;
                batch.Clear();
            }
        }
        if (batch.Count > 0) yield return batch;
    }

    private async Task<long> FetchLatestGlobalPositionAsync(long globalPosition, CancellationToken cancellationToken = default)
    {
        List<StreamEntity> entities = [];
        await foreach (var entity in tableClient.QueryAsync<StreamEntity>(e => e.RowKey == options.StreamEntityRowKey && e.GlobalSequence >= globalPosition, cancellationToken: cancellationToken)) entities.Add(entity);
        return entities.Count == 0 ? globalPosition : entities.Max(static e => e.GlobalSequence);
    }
}
