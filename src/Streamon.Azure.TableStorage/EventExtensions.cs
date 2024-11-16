using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal static class EventExtensions
{
    public static IEnumerable<EventEnvelope> ToEventEnvelopes(this IEnumerable<EventEntity> eventEntities, TableStreamStoreOptions options) =>
        eventEntities
            .Where(e => e.RowKey.StartsWith(options.EventEntityRowKeyPrefix))
            .Select(e => new EventEnvelope(
                    new EventId(e.EventId), 
                    new(e.Sequence), 
                    new(e.GlobalSequence), 
                    e.CreatedOn,
                    options.StreamTypeProvider.ResolveEvent(e.Type, e.Data),
                    options.StreamTypeProvider.ResolveMetadata(e.Metadata)));

    public static ITableEntity ToEventEntity(this object @event, EventId eventId, StreamId streamId, StreamPosition position, StreamPosition globalPosition, EventMetadata? metadata, IStreamTypeProvider streamTypeProvider, TableStreamStoreOptions options)
    {
        var eventTypeInfo = streamTypeProvider.SerializeEvent(@event);
        return new EventEntity
        {
            PartitionKey = streamId.Value,
            RowKey = position.ToEventEntityRowKey(options),
            Sequence = position.Value,
            GlobalSequence = globalPosition.Value,
            EventId = eventId.Value,
            Data = eventTypeInfo.Data,
            Type = eventTypeInfo.Type,
            Metadata = streamTypeProvider.SerializeMetadata(@event.GetEventMetadata(metadata)),
            CreatedOn = DateTimeOffset.UtcNow,
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

    public static IEnumerable<ITableEntity> ToEventEntityPair(this EventEnvelope eventEnvelope, StreamId streamId, StreamPosition globalPosition, EventMetadata? metadata, IStreamTypeProvider streamTypeProvider, TableStreamStoreOptions options) =>
        [
            eventEnvelope.Payload.ToEventEntity(eventEnvelope.EventId, streamId, eventEnvelope.StreamPosition, globalPosition, metadata, streamTypeProvider, options),
            new EventIdEntity { PartitionKey = streamId.Value, RowKey = eventEnvelope.EventId.ToEventIdEntityRowKey(options) }
        ];

    public static string ToEventIdEntityRowKey(this EventId eventId, TableStreamStoreOptions options) => $"{options.EventIdEntityRowKeyPrefix}{eventId.Value:0}";
    public static string ToEventEntityRowKey(this StreamPosition eventPosition, TableStreamStoreOptions options) => $"{options.EventEntityRowKeyPrefix}{eventPosition.Value:000000000000000000}";
    public static string ToSnapshotEntityRowKey(this Type projectionType, TableStreamStoreOptions options) => $"{options.SnapshotEntityPrefix}{projectionType.Name}";
}
