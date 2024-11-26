namespace Streamon.Subscription;

public class EventConsumeContext<T>(SubscriptionId subscriptionId, StreamId streamId, EventId eventId, T payload)
{
    internal EventConsumeContext(EventConsumeContext<object> untypedContext) :
        this(untypedContext.SubscriptionId, untypedContext.StreamId, untypedContext.EventId, (T)untypedContext.Payload)
    {
        StreamPosition = untypedContext.StreamPosition;
        GlobalPosition = untypedContext.GlobalPosition;
        ContextItems = untypedContext.ContextItems;
        Metadata = untypedContext.Metadata;
    }

    public SubscriptionId SubscriptionId { get; } = subscriptionId;
    public T Payload { get; } = payload;
    public StreamId StreamId { get; } = streamId;
    public EventId EventId { get; } = eventId;
    public StreamPosition StreamPosition { get; set; }
    public StreamPosition GlobalPosition { get; set; }
    public EventMetadata? Metadata { get; set; }
    public Dictionary<string, object> ContextItems { get; } = [];
}
