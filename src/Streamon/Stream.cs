using System.Collections;

namespace Streamon;

public class Stream(StreamId streamId, IEnumerable<EventEnvelope> events, StreamPosition globalPosition) : IEnumerable<EventEnvelope>
{
    public StreamId Id { get; } = streamId;

    public StreamPosition CurrentPosition { get; } = events.Last().StreamPosition;

    public StreamPosition GlobalPosition { get; } = globalPosition;

    public IEnumerator<EventEnvelope> GetEnumerator() => events.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => events.GetEnumerator();
}
