using System.Collections.Immutable;

namespace Streamon.Memory;

public class MemoryStreamStore : IStreamStore
{
    private readonly Dictionary<StreamId, List<EventEnvelope>> _streams = [];

    public Task DeleteStreamAsync(StreamId streamId, StreamPosition expectedSequence, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var events)) throw new StreamNotFoundException(streamId);
        var actualSequence = events.Last().StreamPosition;
        if (actualSequence != expectedSequence) throw new StreamConcurrencyException(expectedSequence, actualSequence);
        _streams.Remove(streamId);
        return Task.CompletedTask;
    }

    public Task<Stream> FetchAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        endPosition = endPosition == default ? StreamPosition.End : endPosition;
        if (!_streams.TryGetValue(streamId, out var existingEvents)) throw new StreamNotFoundException(streamId);
        var eventEnvelopes = existingEvents.Where(e => e.StreamPosition >= startPosition && e.StreamPosition <= endPosition).ToImmutableArray();
        var globalEventPosition = _streams.SelectMany(static s => s.Value).Count();
        return Task.FromResult(new Stream(streamId, GlobalEventPosition, eventEnvelopes.AsEnumerable()));
    }

    public Task<Stream> AppendAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = default, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var existingEvents))
        {
            if (expectedPosition == StreamPosition.Start || expectedPosition == StreamPosition.Any)
            {
                var newEvents = ConvertToEnvelopes(StreamPosition.Start, GlobalEventPosition, events, metadata);
                _streams.Add(streamId, [.. newEvents]);
                return Task.FromResult(new Stream(streamId, GlobalEventPosition, newEvents.ToImmutableArray()));
            }
            else throw new StreamNotFoundException(streamId);
        }

        var lastEvent = existingEvents.LastOrDefault();
        if (lastEvent is null) throw new InvalidStreamStateException("Invalid store state, found an empty stream");
        else if (lastEvent.StreamPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, lastEvent.StreamPosition);
        else
        {
            var newEvents = ConvertToEnvelopes(lastEvent?.StreamPosition ?? StreamPosition.Start, GlobalEventPosition, events, metadata);
            _streams[streamId].AddRange(newEvents);
            return Task.FromResult(new Stream(streamId, GlobalEventPosition, [.. newEvents]));
        }
    }

    private static EventMetadata? ExtractMetadata(object @event) =>
        @event is IHasEventMetadata metadata ? metadata.Metadata : default;

    private static IEnumerable<EventEnvelope> ConvertToEnvelopes(StreamPosition startingSequence, StreamPosition globalPosition, IEnumerable<object> events, EventMetadata? metadata) =>
        events.Select((e, i) => new EventEnvelope(EventId.New(), new(startingSequence.Value + i), new(globalPosition.Value + i), DateTimeOffset.Now, e, metadata ?? ExtractMetadata(e)));

    private StreamPosition GlobalEventPosition { get => new(Math.Max(_streams.SelectMany(s => s.Value).Count() - 1, 0)); }
}
