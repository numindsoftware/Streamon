﻿using System.Collections.Immutable;

namespace Streamon.Memory;

public class MemoryEventStore : IStreamStore
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
        var globalEventPosition = _streams.SelectMany(s => s.Value).Count();
        return Task.FromResult(new Stream(streamId, eventEnvelopes.AsEnumerable(), GlobalEventPosition));
    }

    public Task<Stream> AppendAsync(StreamId streamId, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = default, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(streamId, out var existingEvents))
        {
            if (expectedPosition == StreamPosition.Start || expectedPosition == StreamPosition.Any)
            {
                var newEvents = ConvertToEnvelopes(StreamPosition.Start, events, metadata);
                _streams.Add(streamId, [.. newEvents]);
                return Task.FromResult(new Stream(streamId, newEvents.ToImmutableArray(), GlobalEventPosition));
            }
            else throw new StreamNotFoundException(streamId);
        }

        var lastEvent = existingEvents.LastOrDefault();
        if (lastEvent is null) throw new InvalidStreamStateException("Invalid store state, found an empty stream");
        //if (lastEvent is not null && expectedSequence == StreamPosition.Start)
        //{
        //    throw new StreamConcurrencyException(expectedSequence, lastEvent.StreamPosition, $"Expected an empty stream but found {lastEvent.StreamPosition} event(s)");
        //}
        else if (lastEvent.StreamPosition != expectedPosition) throw new StreamConcurrencyException(expectedPosition, lastEvent.StreamPosition);
        else
        {
            var newEvents = ConvertToEnvelopes(lastEvent?.StreamPosition ?? StreamPosition.Start, events, metadata);
            _streams[streamId].AddRange(newEvents);
            return Task.FromResult(new Stream(streamId, [.. newEvents], GlobalEventPosition));
        }
    }

    private static EventMetadata? ExtractMetadata(object @event) =>
        @event is IHasEventMetadata metadata ? metadata.Metadata : default;

    private static IEnumerable<EventEnvelope> ConvertToEnvelopes(StreamPosition startingSequence, IEnumerable<object> events, EventMetadata? metadata) =>
        events.Select((e, i) => new EventEnvelope(EventId.New(), startingSequence + i, DateTimeOffset.Now, e, metadata ?? ExtractMetadata(e)));

    private StreamPosition GlobalEventPosition { get => new(Math.Max(_streams.SelectMany(s => s.Value).Count() - 1, 0)); }
}
