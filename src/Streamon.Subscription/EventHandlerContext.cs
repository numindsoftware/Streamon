namespace Streamon.Subscription;

/// <summary>
/// Represents the context of an event to be used on handlers, different from the "Event" which is used for event storage and transport.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="streamId"></param>
/// <param name="eventId"></param>
/// <param name="payload"></param>
public record EventHandlerContext<T>(
    SubscriptionId SubscriptionId,
    StreamId StreamId, 
    EventId EventId,
    StreamPosition StreamPosition,
    StreamPosition GlobalPosition,
    DateTimeOffset Timestamp,
    BatchId BatchId,
    T Payload,
    EventMetadata? Metadata = default)
{
    public static EventHandlerContext<T> From(SubscriptionId SubscriptionId, Event @event) =>
        new(
            SubscriptionId,
            @event.StreamId, 
            @event.EventId, 
            @event.StreamPosition,
            @event.GlobalPosition,
            @event.Timestamp,
            @event.BatchId,
            (T)@event.Payload,
            @event.Metadata);
}
