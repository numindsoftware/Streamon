using System.Collections.Immutable;

namespace Streamon.Memory;

public class MemoryStreamStore : IStreamStore
{
    private readonly Dictionary<StreamId, List<Event>> _streams = [];

    public event EventHandler<StreamEventArgs>? EventsAppended;
    public event EventHandler<StreamIdEventArgs>? StreamDeleted;

    public Task<long> DeleteStreamAsync(StreamId streamId, StreamPosition expectedSequence, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var events)) throw new StreamNotFoundException(streamId);
        var actualSequence = events.Last().StreamPosition;
        if (actualSequence != expectedSequence) throw new StreamConcurrencyException(expectedSequence, actualSequence);
        _streams.Remove(streamId);
        OnStreamDeleted(streamId);
        return Task.FromResult<long>(events.Count);
    }

    public Task<IEnumerable<Event>> FetchEventsAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        endPosition = endPosition == default ? StreamPosition.End : endPosition;
        if (!_streams.TryGetValue(streamId, out var existingEvents)) throw new StreamNotFoundException(streamId);
        var eventEnvelopes = existingEvents.Where(e => e.StreamPosition >= startPosition && e.StreamPosition <= endPosition).ToImmutableArray();
        var globalEventPosition = _streams.SelectMany(static s => s.Value).Count();
        return Task.FromResult(eventEnvelopes.AsEnumerable());
    }

    public Task<IEnumerable<Event>> AppendEventsAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = default, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var existingEvents))
        {
            if (expectedPosition == StreamPosition.Start || expectedPosition == StreamPosition.Any)
            {
                var newEvents = ConvertToEnvelopes(streamId, BatchId.New(), StreamPosition.Start, GlobalEventPosition, events, metadata);
                _streams.Add(streamId, [.. newEvents]);
                OnEventsAppended(newEvents);
                return Task.FromResult(newEvents);
            }
            else throw new StreamNotFoundException(streamId);
        }

        var lastEvent = existingEvents.LastOrDefault();
        if (lastEvent is null) throw new InvalidStreamStateException("Invalid store state, found an empty stream");
        else if (lastEvent.StreamPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, lastEvent.StreamPosition);
        else
        {
            var newEvents = ConvertToEnvelopes(streamId, BatchId.New(), lastEvent?.StreamPosition ?? StreamPosition.Start, GlobalEventPosition, events, metadata);
            _streams[streamId].AddRange(newEvents);
            return Task.FromResult(newEvents);
        }
    }

    protected virtual void OnEventsAppended(IEnumerable<Event> events) => EventsAppended?.Invoke(this, new(events));
    protected virtual void OnStreamDeleted(StreamId streamId) => StreamDeleted?.Invoke(this, new(streamId));

    private static EventMetadata? ExtractMetadata(object @event) =>
        @event is IHasEventMetadata metadata ? metadata.Metadata : default;

    private static IEnumerable<Event> ConvertToEnvelopes(StreamId streamId, BatchId batchId, StreamPosition startingSequence, StreamPosition globalPosition, IEnumerable<object> events, EventMetadata? metadata) =>
        events.Select((e, i) => new Event(streamId, EventId.New(), StreamPosition.From(startingSequence.Value + i), StreamPosition.From(globalPosition.Value + i), DateTimeOffset.Now, batchId, e, metadata ?? ExtractMetadata(e)));

    private StreamPosition GlobalEventPosition { get => StreamPosition.From(Math.Max(_streams.SelectMany(s => s.Value).Count() - 1, 0)); }
}
