using Microsoft.Azure.Cosmos;

namespace Streamon.Azure.CosmosDb;

internal class CosmosDbStreamStore(Container container, CosmosDbStreamStoreOptions options) : IStreamStore
{
    public event EventHandler<StreamEventArgs>? EventsAppended;
    public event EventHandler<StreamIdEventArgs>? StreamDeleted;

    public async Task<Stream> AppendAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        double requestCharge = 0;
        var streamResponse = await container.ReadItemAsync<StreamDocument>(streamId.Value, new PartitionKey(streamId.Value), cancellationToken: cancellationToken);
        requestCharge += streamResponse.RequestCharge;
        var streamDocument = streamResponse.Resource;
        if (streamDocument == null)
        {
            if (expectedPosition != StreamPosition.Any && expectedPosition != StreamPosition.Start && expectedPosition != StreamPosition.End) throw new StreamNotFoundException(streamId);
            streamDocument = new(streamId, StreamPosition.Start, StreamPosition.Start, DateTime.UtcNow, DateTime.UtcNow);
        }
        else if (streamDocument.Position != expectedPosition) 
        {
            throw new StreamConcurrencyException(expectedPosition, streamDocument.Position);
        }

        var eventEnvelopes = new List<EventEnvelope>();
        var transaction = container.CreateTransactionalBatch(new PartitionKey(streamId.Value));
        var batchId = BatchId.New();
        foreach (var @event in events)
        {
            streamDocument = streamDocument with
            {
                Position = streamDocument.Position.Next(),
                GlobalPosition = streamDocument.GlobalPosition.Next(),
            };
            var eventId = @event.GetEventId();
            var eventTypeInfo = options.StreamTypeProvider.SerializeEvent(@event);
            EventDocument eventDocument = new(
                streamId.ToStreamDocumentId(eventId), 
                eventId, 
                streamDocument.Position, 
                streamDocument.GlobalPosition, 
                batchId, 
                DateTimeOffset.Now,
                eventTypeInfo.Type,
                eventTypeInfo.Data,
                metadata);
            transaction.CreateItem(eventDocument);
            eventEnvelopes.Add(new EventEnvelope(streamId, eventId, eventDocument.Position, eventDocument.GlobalPosition, DateTimeOffset.Now, batchId, @event, metadata));
        }

        transaction.ReplaceItem(streamId.Value, streamDocument);
        var batchResponse = await transaction.ExecuteAsync(cancellationToken);
        requestCharge += batchResponse.RequestCharge;
        if (!batchResponse.IsSuccessStatusCode) throw new CosmosDbOperationException($"Could not save events batch with error code: {batchResponse.StatusCode}");
        Stream stream = new(streamId, streamDocument.GlobalPosition, eventEnvelopes);
        OnEventsAppended(new(streamId, streamDocument.Position, eventEnvelopes));
        return stream;
    }

    public async Task<long> DeleteStreamAsync(StreamId streamId, StreamPosition expectedPosition, CancellationToken cancellationToken = default)
    {
        double requestCharge = 0;
        var streamResponse = await container.ReadItemAsync<StreamDocument>(streamId.Value, new PartitionKey(streamId.Value), cancellationToken: cancellationToken);
        requestCharge += streamResponse.RequestCharge;
        if (streamResponse.Resource == null) throw new StreamNotFoundException(streamId);
        var streamDocument = streamResponse.Resource;
        if (streamDocument.Position != expectedPosition) throw new StreamConcurrencyException(expectedPosition, streamDocument.Position);
        if (options.DeleteMode == StreamDeleteMode.Hard)
        {
            var response = await container.DeleteAllItemsByPartitionKeyStreamAsync(new PartitionKey(streamId.Value), cancellationToken: cancellationToken);
            response.ThrowOnError($"Could not delete stream with error code: {response.StatusCode}");
            return streamDocument.Position.Value;
        }
        else
        {
            streamDocument = streamDocument with
            {
                IsDeleted = true,
                DeletedOn = DateTimeOffset.Now,
                UpdatedOn = DateTimeOffset.Now,
            };
            var response = await container.ReplaceItemAsync(streamDocument, streamId.Value, new PartitionKey(streamId.Value), cancellationToken: cancellationToken);
            requestCharge += response.RequestCharge;
            response.ThrowOnError($"Could not delete stream with error code: {response.StatusCode}");
        }
        return streamDocument.Position.Value;
    }

    public async Task<Stream> FetchAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        endPosition = endPosition == default ? StreamPosition.End : endPosition;

        var feedIterator = container.GetItemQueryIterator<StreamDocument>(new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", streamId.Value));
        double requestCharge = 0;
        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            requestCharge += response.RequestCharge;
            foreach (var document in response)
            {
                
            }

            var stream = response.FirstOrDefault();
            if (stream != null)
            {
            }
        }

        return new Stream(streamId, startPosition, []);
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
}
