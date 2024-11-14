using Azure.Data.Tables;
using System.Text.Json;

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

    public static StreamEntity ExtractStreamEntity(this IEnumerable<TableEntity> entities)
    {
        var entity = entities.SingleOrDefault(e => e.RowKey == StreamEntity.StreamRowKey) 
            ?? throw new TableStorageOperationException("Stream entity not found");
        StreamEntity streamEntity = new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            CurrentSequence = entity.GetInt64Value(nameof(StreamEntity.CurrentSequence)),
            UpdatedOn = entity.GetDateTimeOffsetValue(nameof(StreamEntity.UpdatedOn)),
            GlobalSequence = entity.GetInt64Value(nameof(StreamEntity.GlobalSequence)),
            CreatedOn = entity.GetDateTimeOffsetValue(nameof(StreamEntity.CreatedOn)),
            DeletedOn = entity.GetDateTimeOffset(nameof(StreamEntity.DeletedOn)),
            IsDeleted = entity.GetBoolean(nameof(StreamEntity.IsDeleted)) ?? false
        };
        return streamEntity;
    }

    public static IEnumerable<EventEntity> ExtractEventEntities(this IEnumerable<TableEntity> entities)
    {
        var eventEntities = entities
            .Where(e => e.RowKey.StartsWith(EventEntity.EventRowKeyPrefix))
            .Select(e => e.ToEventEntity()!);
        if (!eventEntities.Any()) throw new InvalidStreamStateException("No events found in stream");
        return eventEntities;
    }

    //public static IEnumerable<EventEnvelope> ToEventEnvelopes(this IEnumerable<EventEntity> eventEntities, JsonSerializerOptions serializerOptions) =>
    //    eventEntities.Select(e => e.ToEventEnvelope(serializerOptions));

    public static EventEntity ToEventEntity(this TableEntity entity) =>
        new()
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Sequence = entity.GetInt64Value(nameof(EventEntity.Sequence)),
            CreatedOn = entity.GetDateTimeOffsetValue(nameof(EventEntity.CreatedOn)),
            Type = entity.GetString(nameof(EventEntity.Type)) ?? string.Empty,
            EventId = entity.GetString(nameof(EventEntity.EventId)) ?? string.Empty,
            Data = entity.GetString(nameof(EventEntity.Data)) ?? string.Empty,
            Metadata = entity.GetString(nameof(EventEntity.Metadata)),
        };

    public static ITableEntity ToEventEntity(this object @event, StreamId streamId, StreamPosition position, JsonSerializerOptions serializerOptions) =>
        new EventEntity
        {
            PartitionKey = streamId.Value,
            RowKey = string.Format(EventEntity.EventRowKeyFormat, position),
            Sequence = position.Value,
            EventId = @event.GetEventId().Value,
            Data = @event.GetEventDataSerialized(serializerOptions),
            Type = @event.GetEventType(),
            Metadata = @event.GetEventMetadataSerialized(serializerOptions),
            CreatedOn = DateTimeOffset.UtcNow,
        };

    public static ITableEntity ToEventIdEntity(this object @event, StreamId streamId, StreamPosition position) =>
        new EventIdEntity
        {
            PartitionKey = streamId.Value,
            RowKey = string.Format(EventIdEntity.EventIdRowKeyFormat, @event.GetEventId()),
            Sequence = position.Value,
        };

    public static StreamEntity ToStreamEntity(this StreamId streamId, StreamPosition currentPosition, StreamPosition globalPosition) =>
        new()
        {
            PartitionKey = streamId.Value,
            RowKey = StreamEntity.StreamRowKey,
            CurrentSequence = currentPosition.Value,
            UpdatedOn = DateTimeOffset.UtcNow,
            GlobalSequence = globalPosition.Value,
            CreatedOn = DateTimeOffset.UtcNow
        };

    public static string GetEventDataSerialized(this object @event, JsonSerializerOptions serializerOptions) =>
        JsonSerializer.Serialize(@event, serializerOptions);

    public static string? GetEventMetadataSerialized(this object @event, JsonSerializerOptions serializerOptions) =>
        @event is IHasEventMetadata metadata ? JsonSerializer.Serialize(metadata.Metadata, serializerOptions) : null;

    public static IEnumerable<ITableEntity> ToEventEntityPair(this EventEnvelope eventEnvelope, StreamId streamId, JsonSerializerOptions serializerOptions)
        =>
        [
            eventEnvelope.Payload.ToEventEntity(streamId, eventEnvelope.StreamPosition, serializerOptions),
            eventEnvelope.Payload.ToEventIdEntity(streamId, eventEnvelope.StreamPosition)
        ];
}
