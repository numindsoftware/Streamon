using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace Streamon.Azure.TableStorage;

public class TableStreamStore(TableClient tableClient, TableStreamStoreOptions options) : IStreamStore
{
    public event EventHandler<StreamEventArgs>? EventsAppended;
    public event EventHandler<StreamIdEventArgs>? StreamDeleted;

    public async Task<IEnumerable<Event>> FetchEventsAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        endPosition = endPosition == default ? StreamPosition.End : endPosition;
        
        var streamEntityResponse = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, options.StreamEntityRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!streamEntityResponse.HasValue) throw new StreamNotFoundException(streamId);

        List<EventEntity> entities = [];
        await foreach (var entity in tableClient.QueryAsync<EventEntity>(e => e.PartitionKey == streamId.Value && e.Sequence >= startPosition.Value && e.Sequence <= endPosition.Value, cancellationToken: cancellationToken)) entities.Add(entity);
        if (entities.Count == 0) throw new StreamNotFoundException(streamId);
        
        return entities.ToEvents(options);
    }

    public async Task<IEnumerable<Event>> AppendEventsAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
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
        List<Event> eventEnvelopes = [];
        foreach (var @event in events)
        {
            currentPosition = currentPosition.Next();
            globalPosition = globalPosition.Next();
            var eventEnvelope = @event.ToEvent(streamId, batchId, currentPosition, globalPosition, metadata);
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
        return eventEnvelopes;
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

    protected virtual void OnEventsAppended(IEnumerable<Event> events)
    {
        options.OnEventsAppended?.Invoke(events);
        EventsAppended?.Invoke(this, new(events));
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

    /// <summary>
    /// This is extremely inneficient, a full table scan is performed to get the latest global position.
    /// A better approach would be to store the latest global position in a separate entity and update it on each append.
    /// </summary>
    /// <param name="globalPosition"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<long> FetchLatestGlobalPositionAsync(long globalPosition, CancellationToken cancellationToken = default)
    {
        List<StreamEntity> entities = [];
        await foreach (var page in tableClient.QueryAsync<StreamEntity>(e => e.RowKey == options.StreamEntityRowKey && e.GlobalSequence >= globalPosition, cancellationToken: cancellationToken).AsPages())
            globalPosition = Math.Max(page.Values.Select(e => e.GlobalSequence).DefaultIfEmpty(globalPosition).Max(), globalPosition);
            
        return globalPosition;
    }
}
