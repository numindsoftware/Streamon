namespace Streamon.Tests;

public class EventMarkersTests
{
    [Fact]
    public void ShouldExtractEventIdFromAttribute()
    {
        var @event = new TestEventWithAttributeId(EventId.New());
        var eventId = EventExtensions.GetEventId(@event);
        Assert.False(string.IsNullOrEmpty(eventId.Value));
    }

    [Fact]
    public void ShouldExtractEventIdStringFromAttribute()
    {
        var @event = new TestEventWithAttributeStringId(EventId.New().Value);
        var eventId = EventExtensions.GetEventId(@event);
        Assert.False(string.IsNullOrEmpty(eventId.Value));
    }

    [Fact]
    public void ShouldExtractEventIdFromInterface()
    {
        var @event = new TestEventWithInterfaceId(EventId.New());
        var eventId = EventExtensions.GetEventId(@event);
        Assert.False(string.IsNullOrEmpty(eventId.Value));
    }

    [Fact]
    public void ShouldExtractMetadataFromInterface()
    {
        var @event = new TestEventWithMetadata(new EventMetadata { { "key", "value" } });
        var metadata = EventExtensions.GetEventMetadata(@event);
        Assert.NotNull(metadata);
    }

    public record TestEventWithAttributeId([property: EventId] EventId Id);
    public record TestEventWithAttributeStringId([property: EventId] string Id);
    public record TestEventWithInterfaceId(EventId EventId) : IHasEventId;
    public record TestEventWithMetadata(EventMetadata Metadata) : IHasEventMetadata;
}
