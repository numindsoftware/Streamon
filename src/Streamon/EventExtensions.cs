using System.Reflection;
using System.Text.Json;

namespace Streamon;

public static class EventExtensions
{
    private static readonly Dictionary<Type, MemberInfo> EventIdMembersMap = [];

    public static EventId GetEventId(this object @event)
    {
        if (@event is IHasEventId indentifiable) return indentifiable.EventId;
        if (!EventIdMembersMap.TryGetValue(@event.GetType(), out MemberInfo? eventIdMember))
        {
            eventIdMember = @event.GetType()
                .GetMembers(BindingFlags.Instance)
                .Where(mi => mi.GetCustomAttribute<EventIdAttribute>() != null)
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

    public static string GetEventType(this object @event) =>
        @event.GetType().GetCustomAttribute<EventTypeAttribute>()?.Name ?? @event.GetType().Name;

    public static EventEnvelope ToEventEnvelope(this object @event, StreamPosition position, DateTimeOffset timestamp, EventMetadata? metadata)
    {
        return new(
            @event.GetEventId(),
            position,
            timestamp,
            @event,
            metadata);
    }
}
