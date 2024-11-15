using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System.IO;

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

        return new Stream(streamId, new(streamEntity.Sequence), [.. eventEnvelopes]);
    }

    public async Task<Stream> AppendAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var streamResult = await tableClient.GetEntityIfExistsAsync<StreamEntity>(streamId.Value, StreamEntity.StreamRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (streamResult.HasValue && streamResult.Value!.IsDeleted) throw new StreamDeletedException(streamId, $"The Stream {streamId} has been deleted.");
        var streamEntity = streamResult.HasValue ? streamResult.Value! : streamId.ToStreamEntity(StreamPosition.Start, StreamPosition.Start);
        
        StreamPosition currentPosition = new(streamEntity.Sequence);
        if (currentPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, currentPosition);

        // global position can be useful when projecting to event streams
        var globalPosition = _options.CalculateGlobalPosition
                ? (await FetchLatestGlobalPositionAsync(cancellationToken).ConfigureAwait(false))
                : currentPosition;

        List<EventEnvelope> envelopes = [];
        foreach (var batch in GenerateSaveBatchAsync(streamId, currentPosition, globalPosition, events, metadata))
        {
            globalPosition = batch.GlobalPosition;
            streamEntity.Sequence = batch.CurrentPosition.Value;
            streamEntity.GlobalSequence = batch.GlobalPosition.Value;
            streamEntity.UpdatedOn = DateTimeOffset.UtcNow;
            TableTransactionAction[] streamTransactions = [new(TableTransactionActionType.UpsertMerge, streamEntity), .. batch.Transactions];
            try
            {
                var response = await tableClient.SubmitTransactionAsync(streamTransactions, cancellationToken).ConfigureAwait(false);
                response.Value.ToList().ForEach(r => r.ThrowOnError($"Failed to upsert stream with status code {r.Status}"));
                // update the ETag for the next batch, needed otherwise we fail due to concurrency control
                streamEntity.ETag = response.Value[0].Headers.ETag!.Value;
                envelopes.AddRange(batch.EventEnvelopes);
            }
            catch (TableTransactionFailedException ex) when (ex.ErrorCode == TableErrorCode.EntityAlreadyExists)
            {
                var entityId = new EventId(streamTransactions[ex.FailedTransactionActionIndex!.Value].Entity.RowKey.Replace(EventIdEntity.EventIdRowKeyPrefix, string.Empty));
                throw new DuplicateEventException(entityId);
            }
            catch (TableTransactionFailedException ex)
            {
                throw new TableStorageOperationException($"An error occurred while saving events. {ex.Message}", ex);
            }
        }
        return new Stream(streamId, globalPosition, envelopes);
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
        await foreach (var entity in tableClient.QueryAsync<StreamEntity>(static e => e.RowKey == StreamEntity.StreamRowKey, cancellationToken: cancellationToken)) entities.Add(entity);
        return entities.Count == 0 ? StreamPosition.Start : new(entities.Sum(static e => e.Sequence));
    }

    private IEnumerable<TransactionBatch> GenerateSaveBatchAsync(StreamId streamId, StreamPosition startPosition, StreamPosition globalPosition, IEnumerable<object> events, EventMetadata? metadata = default)
    {
        TransactionBatch batch = new(streamId, startPosition, globalPosition, streamTypeProvider, metadata, _options.TransactionBatchSize);
        foreach (var @event in events)
        {
            if (batch.AddTransactions(@event))
            {
                yield return batch;
                batch.Reset();
            }
        }
        if (batch.Transactions.Any()) yield return batch;
    }

    private class TransactionBatch(StreamId streamId, StreamPosition startPosition, StreamPosition globalPosition, IStreamTypeProvider streamTypeProvider, EventMetadata? metadata, byte maxBatchSize)
    {
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
            var eventEnvelope = @event.ToEventEnvelope(CurrentPosition, GlobalPosition, DateTimeOffset.UtcNow, metadata);
            var transactions = eventEnvelope
                .ToEventEntityPair(streamId, GlobalPosition, metadata, streamTypeProvider)
                .Select(e => new TableTransactionAction(TableTransactionActionType.Add, e));
            _tableTransactionActions.AddRange(transactions);
            _eventEnvelopes.Add(eventEnvelope);
            // what follow is to ensure that we don't exceed the max batch size while accounting for the fact that we need to add the stream entity as well
            // The modulus operation is to ensure that we don't end up with a larger number of transactions than the max batch size
            return _tableTransactionActions.Count >= (maxBatchSize + (maxBatchSize % 2) - 2);
        }
    }
}
