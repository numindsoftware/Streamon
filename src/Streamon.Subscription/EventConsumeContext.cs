namespace Streamon.Subscription;

public class EventConsumeContext<T>(StreamId streamId, EventId eventId, T payload)
{
    public static EventConsumeContext<T> From(Event @event) =>
        new(@event.StreamId, @event.EventId, (T)@event.Payload)
        {
            StreamPosition = @event.StreamPosition,
            GlobalPosition = @event.GlobalPosition,
            Metadata = @event.Metadata,
            Timestamp = @event.Timestamp
        };

    //internal EventConsumeContext(EventConsumeContext<object> untypedContext) :
    //    this(untypedContext.StreamId, untypedContext.EventId, (T)untypedContext.Payload)
    //{
    //    StreamPosition = untypedContext.StreamPosition;
    //    GlobalPosition = untypedContext.GlobalPosition;
    //    ContextItems = untypedContext.ContextItems;
    //    Metadata = untypedContext.Metadata;
    //}
    public T Payload { get; } = payload;
    public StreamId StreamId { get; } = streamId;
    public EventId EventId { get; } = eventId;
    public StreamPosition StreamPosition { get; set; }
    public StreamPosition GlobalPosition { get; set; }
    public EventMetadata? Metadata { get; set; }
    //public Dictionary<string, object> ContextItems { get; } = [];
    public DateTimeOffset Timestamp { get; set; }
}
