using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal static class EventExtensions
{
    public static Event ToEvent(this EventEntity eventEntity, IStreamTypeProvider typeProvider) =>
        new(
            StreamId.From(eventEntity.PartitionKey),
            EventId.From(eventEntity.EventId),
            StreamPosition.From(eventEntity.Sequence),
            StreamPosition.From(eventEntity.GlobalSequence),
            eventEntity.CreatedOn,
            BatchId.From(eventEntity.BatchId),
            typeProvider.ResolveEvent(eventEntity.Type, eventEntity.Data),
            typeProvider.ResolveMetadata(eventEntity.Metadata));

    public static IEnumerable<Event> ToEvents(this IEnumerable<EventEntity> eventEntities, TableStreamStoreOptions options) =>
        eventEntities
            .Where(e => e.RowKey.StartsWith(options.EventEntityRowKeyPrefix))
            .Select(e => e.ToEvent(options.StreamTypeProvider));

    public static ITableEntity ToEventEntity(this object @event, EventId eventId, StreamId streamId, BatchId batchId, StreamPosition position, StreamPosition globalPosition, EventMetadata? metadata, IStreamTypeProvider streamTypeProvider, TableStreamStoreOptions options)
    {
        var eventTypeInfo = streamTypeProvider.SerializeEvent(@event);
        return new EventEntity
        {
            PartitionKey = streamId.Value,
            RowKey = position.ToEventEntityRowKey(options),
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

    public static string ToEventEntityRowKey(this StreamPosition position, TableStreamStoreOptions options) =>
        $"{options.EventEntityRowKeyPrefix}{position.Value:00000000000000000000}";

    public static string ToEventIdEntityRowKey(this EventId eventId, TableStreamStoreOptions options) => 
        $"{options.EventIdEntityRowKeyPrefix}{eventId.Value:0}";

    public static string ToSnapshotEntityRowKey(this Type projectionType, TableStreamStoreOptions options) => 
        $"{options.SnapshotEntityPrefix}{projectionType.Name}";
}
