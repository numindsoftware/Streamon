using System.Collections;

namespace Streamon;

public class Stream(StreamId streamId, StreamPosition globalPosition, IEnumerable<EventEnvelope> events) : IEnumerable<EventEnvelope>
{
    public StreamId Id { get; } = streamId;

    public StreamPosition CurrentPosition { get; } = events.Max(e => e.StreamPosition);

    public StreamPosition GlobalPosition { get; } = globalPosition;

    public IEnumerator<EventEnvelope> GetEnumerator() => events.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
