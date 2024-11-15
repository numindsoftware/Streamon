using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal static class EventExtensions
{
    public static T ThrowIfNullEntityValue<T>(this Nullable<T> value, string paramName, string streamId) where T : struct =>
        value ?? throw new TableStorageOperationException($"Could not find a {paramName} field in Stream {streamId}");
    public static T ThrowIfNullEntityValue<T>(this T? value, string paramName, string streamId) where T : class =>
        value ?? throw new TableStorageOperationException($"Could not find a {paramName} field in Stream {streamId}");

    public static int GetInt32Value(this TableEntity entity, string key) => entity.GetInt32(key).ThrowIfNullEntityValue(key, entity.PartitionKey);
    public static long GetInt64Value(this TableEntity entity, string key) => entity.GetInt64(key).ThrowIfNullEntityValue(key, entity.PartitionKey);
    public static DateTimeOffset GetDateTimeOffsetValue(this TableEntity entity, string key) => entity.GetDateTimeOffset(key).ThrowIfNullEntityValue(key, entity.PartitionKey);
    public static long GetBoolean(this TableEntity entity, string key) => entity.GetInt64(key).ThrowIfNullEntityValue(key, entity.PartitionKey);

    public static StreamEntity ExtractStreamEntity(this IEnumerable<TableEntity> entities, TableStreamStoreOptions options)
    {
        var entity = entities.SingleOrDefault(e => e.RowKey == options.StreamEntityRowKey) 
            ?? throw new TableStorageOperationException("Stream entity not found");
        StreamEntity streamEntity = new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Sequence = entity.GetInt64Value(nameof(StreamEntity.Sequence)),
            UpdatedOn = entity.GetDateTimeOffsetValue(nameof(StreamEntity.UpdatedOn)),
            GlobalSequence = entity.GetInt64Value(nameof(StreamEntity.GlobalSequence)),
            CreatedOn = entity.GetDateTimeOffsetValue(nameof(StreamEntity.CreatedOn)),
            DeletedOn = entity.GetDateTimeOffset(nameof(StreamEntity.DeletedOn)),
            IsDeleted = entity.GetBoolean(nameof(StreamEntity.IsDeleted)) ?? false
        };
        return streamEntity;
    }

    public static IEnumerable<EventEnvelope> ToEventEnvelopes(this IEnumerable<TableEntity> eventEntities, IStreamTypeProvider streamTypeProvider, TableStreamStoreOptions options) =>
        eventEntities
            .Where(e => e.RowKey.StartsWith(options.EventEntityRowKeyPrefix))
            .Select(e => new EventEnvelope(
                    new EventId(e.GetString(nameof(EventEntity.EventId))),
                    new StreamPosition(e.GetInt64Value(nameof(EventEntity.Sequence))),
                    new StreamPosition(e.GetInt64Value(nameof(EventEntity.GlobalSequence))),
                    e.GetDateTimeOffsetValue(nameof(EventEntity.CreatedOn)),
                    streamTypeProvider.ResolveEvent(e.GetString(nameof(EventEntity.Type)), e.GetString(nameof(EventEntity.Data))),
                    streamTypeProvider.ResolveMetadata(e.GetString(nameof(EventEntity.Metadata)))));

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
