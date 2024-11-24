using System.Reflection;

namespace Streamon;

public static class EventExtensions
{
    private static readonly Dictionary<Type, MemberInfo> EventIdMembersMap = [];

    public static StreamPosition CurrentPosition(this IEnumerable<Event> events) =>
        events.Any() ? events.Max(static e => e.StreamPosition) : StreamPosition.Start;

    public static StreamPosition GlobalPosition(this IEnumerable<Event> events) =>
        events.Any() ? events.Max(static e => e.GlobalPosition) : StreamPosition.Start;

    public static EventId GetEventId(this object @event)
    {
        if (@event is IHasEventId indentifiable) return indentifiable.EventId;
        if (!EventIdMembersMap.TryGetValue(@event.GetType(), out MemberInfo? eventIdMember))
        {
            eventIdMember = @event.GetType()
                .GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(static mi => mi.GetCustomAttribute<EventIdAttribute>() != null)
                .FirstOrDefault();
            if (eventIdMember is not null)
            {
                EventIdMembersMap[@event.GetType()] = eventIdMember;
            }
        }

        return eventIdMember switch
        {
            PropertyInfo mi and { CanRead: true } when mi.GetValue(@event) is EventId eventId => eventId,
            PropertyInfo mi and { CanRead: true } when mi.GetValue(@event) is object value and not null=> new EventId(value.ToString()!),
            FieldInfo mi when mi.GetValue(@event) is EventId eventId => eventId,
            FieldInfo mi when mi.GetValue(@event) is object value and not null => new EventId(value.ToString()!),
            MethodInfo mi when mi.Invoke(@event, []) is EventId eventId => eventId,
            MethodInfo mi when mi.Invoke(@event, []) is object value and not null => new EventId(value.ToString()!),
            _ => EventId.New()
        };
    }

    public static Event ToEventEnvelope(this object @event, StreamId streamId, BatchId batchId, StreamPosition position, StreamPosition globalPosition, EventMetadata? metadata) =>
        new(
            streamId,
            @event.GetEventId(),
            position,
            globalPosition,
            DateTimeOffset.Now,
            batchId,
            @event,
            metadata);

    public static EventMetadata? GetEventMetadata(this object @event, EventMetadata? defaultValue = default) =>
        (@event is IHasEventMetadata identifiable ? identifiable.Metadata : defaultValue) ?? defaultValue;
}
