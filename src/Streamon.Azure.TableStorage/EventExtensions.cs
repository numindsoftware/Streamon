using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal static class EventExtensions
{
    public static IEnumerable<EventEnvelope> ToEventEnvelopes(this IEnumerable<EventEntity> eventEntities, TableStreamStoreOptions options) =>
        eventEntities
            .Where(e => e.RowKey.StartsWith(options.EventEntityRowKeyPrefix))
            .Select(e => new EventEnvelope(
                EventId.From(e.EventId), 
                StreamPosition.From(e.Sequence),
                StreamPosition.From(e.GlobalSequence),
                e.CreatedOn,
                BatchId.From(e.BatchId),
                options.StreamTypeProvider.ResolveEvent(e.Type, e.Data),
                options.StreamTypeProvider.ResolveMetadata(e.Metadata)));

    public static ITableEntity ToEventEntity(this object @event, EventId eventId, StreamId streamId, BatchId batchId, StreamPosition position, StreamPosition globalPosition, EventMetadata? metadata, IStreamTypeProvider streamTypeProvider, TableStreamStoreOptions options)
    {
        var eventTypeInfo = streamTypeProvider.SerializeEvent(@event);
        return new EventEntity
        {
            PartitionKey = streamId.Value,
            RowKey = $"{options.EventEntityRowKeyPrefix}{eventId}",
            Sequence = position.Value,
            GlobalSequence = globalPosition.Value,
            EventId = eventId.Value,
            BatchId = batchId.Value,
            Data = eventTypeInfo.Data,
            Type = eventTypeInfo.Type,
            Metadata = streamTypeProvider.SerializeMetadata(@event.GetEventMetadata(metadata)),
            CreatedOn = DateTimeOffset.Now,
        };
    }

    public static StreamEntity ToStreamEntity(this StreamId streamId, StreamPosition currentPosition, StreamPosition globalPosition, TableStreamStoreOptions options) =>
        new()
        {
            PartitionKey = streamId.Value,
            RowKey = options.StreamEntityRowKey,
            Sequence = currentPosition.Value,
            UpdatedOn = DateTimeOffset.UtcNow,
            GlobalSequence = globalPosition.Value,
            CreatedOn = DateTimeOffset.UtcNow
        };

    public static string ToSnapshotEntityRowKey(this Type projectionType, TableStreamStoreOptions options) => $"{options.SnapshotEntityPrefix}{projectionType.Name}";
}
