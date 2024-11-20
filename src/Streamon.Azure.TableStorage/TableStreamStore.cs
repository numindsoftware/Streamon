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

        var streamResult = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, options.StreamEntityRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (streamResult.HasValue && streamResult.Value!.IsDeleted) throw new StreamDeletedException(streamId, $"The Stream {streamId} has been deleted.");
        var streamEntity = streamResult.HasValue ? streamResult.Value! : streamId.ToStreamEntity(StreamPosition.Start, StreamPosition.Start, options);
        
        StreamPosition currentPosition = StreamPosition.From(streamEntity.Sequence);
        if (currentPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, currentPosition);

        // global position can be useful when projecting to event streams
        var globalPosition = options.CalculateGlobalPosition
                ? (await FetchLatestGlobalPositionAsync(cancellationToken).ConfigureAwait(false))
                : currentPosition;

        //var batch = GenerateSaveBatch(streamId, currentPosition, globalPosition, events, metadata);

        TransactionBatch batch = new(streamId, currentPosition, globalPosition, options.StreamTypeProvider, metadata, options);
        foreach (var @event in events)
        {
            if (batch.AddTransactions(@event)) throw new BatchSizeExceededException(batch.Transactions.Count(), options.TransactionBatchSize, $"The number of events to append exceeds the maximum batch size of {options.TransactionBatchSize}.");
        }
        streamEntity.Sequence = batch.CurrentPosition.Value;
        streamEntity.GlobalSequence = batch.GlobalPosition.Value;
        streamEntity.UpdatedOn = DateTimeOffset.UtcNow;
        TableTransactionAction[] streamTransactions = [new(TableTransactionActionType.UpsertMerge, streamEntity), .. batch.Transactions];
        try
        {
            var response = await tableClient.SubmitTransactionAsync(streamTransactions, cancellationToken).ConfigureAwait(false);
            response.Value.ToList().ForEach(r => r.ThrowOnError($"Failed to upsert stream with status code {r.Status}"));
        }
        catch (TableTransactionFailedException ex) when (ex.ErrorCode == TableErrorCode.EntityAlreadyExists || ex.ErrorCode == TableErrorCode.InvalidDuplicateRow)
        {
            var entityId = new EventId(streamTransactions[ex.FailedTransactionActionIndex!.Value].Entity.RowKey.Replace(options.EventIdEntityRowKeyPrefix, string.Empty));
            throw new DuplicateEventException(entityId);
        }
        catch (RequestFailedException ex)
        {
            throw new TableStorageOperationException($"An error occurred while saving events. {ex.Message}", ex);
        }
        return new Stream(streamId, batch.GlobalPosition, batch.EventEnvelopes);
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
            await foreach(var deleteEntity in tableClient.QueryAsync<ITableEntity>(e => e.PartitionKey == streamId.Value, cancellationToken: cancellationToken)) deletableEntities.Add(deleteEntity);
            foreach (var batch in GenerateDeleteBatch(deletableEntities))
            {
                var response = await tableClient.SubmitTransactionAsync(batch, cancellationToken: cancellationToken).ConfigureAwait(false);
                response.Value.ToList().ForEach(r => r.ThrowOnError($"Failed to delete stream entities with status code {r.Status}"));
            }
            return deletableEntities.Count;
        }
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

    private async Task<StreamPosition> FetchLatestGlobalPositionAsync(CancellationToken cancellationToken = default)
    {
        List<StreamEntity> entities = [];
        await foreach (var entity in tableClient.QueryAsync<StreamEntity>(e => e.RowKey == options.StreamEntityRowKey, cancellationToken: cancellationToken)) entities.Add(entity);
        return entities.Count == 0 ? StreamPosition.Start : StreamPosition.From(entities.Sum(static e => e.Sequence));
    }

    private class TransactionBatch(StreamId streamId, StreamPosition startPosition, StreamPosition globalPosition, IStreamTypeProvider streamTypeProvider, EventMetadata? metadata, TableStreamStoreOptions options)
    {
        private readonly BatchId _batchId = BatchId.New();
        private readonly List<TableTransactionAction> _tableTransactionActions = [];
        private readonly List<EventEnvelope> _eventEnvelopes = [];

        public StreamPosition CurrentPosition { get; private set; } = startPosition;
        public StreamPosition GlobalPosition { get; private set; } = globalPosition;
        public IEnumerable<TableTransactionAction> Transactions { get => _tableTransactionActions; }
        public IEnumerable<EventEnvelope> EventEnvelopes { get => _eventEnvelopes; }
        public void Reset()
        {
            _tableTransactionActions.Clear();
            _eventEnvelopes.Clear();
        }
        public bool AddTransactions(object @event)
        {
            CurrentPosition = CurrentPosition.Next();
            GlobalPosition = GlobalPosition.Next();
            var eventEnvelope = @event.ToEventEnvelope(_batchId, CurrentPosition, GlobalPosition, DateTimeOffset.UtcNow, metadata);
            var transactions = eventEnvelope
                .ToEventEntityPair(streamId, _batchId, GlobalPosition, metadata, streamTypeProvider, options)
                .Select(e => new TableTransactionAction(TableTransactionActionType.Add, e));
            _tableTransactionActions.AddRange(transactions);
            _eventEnvelopes.Add(eventEnvelope);
            // what follow is to ensure that we don't exceed the max batch size while accounting for the fact that we need to add the stream entity as well
            // The modulus operation is to ensure that we don't end up with a larger number of transactions than the max batch size
            return _tableTransactionActions.Count >= (options.TransactionBatchSize + (options.TransactionBatchSize % 2) - 2);
        }
    }
}
