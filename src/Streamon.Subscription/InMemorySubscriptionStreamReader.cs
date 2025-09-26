using System.Collections.Concurrent;

namespace Streamon.Subscription;

/// <summary>
/// An in-memory implementation of ISubscriptionStreamReader that stores events in memory
/// and allows for simulated event streams without persistence requirements
/// </summary>
public class InMemorySubscriptionStreamReader : ISubscriptionStreamReader
{
    private readonly ConcurrentDictionary<long, EventRecord> _events = new();
    private long _currentPosition = 0;

    public record EventRecord(
        StreamId StreamId,
        EventId EventId,
        object Payload,
        EventMetadata Metadata,
        long GlobalPosition,
        long StreamPosition,
        DateTimeOffset Timestamp);

    /// <summary>
    /// Publishes an event to the in-memory event stream
    /// </summary>
    public Task PublishEventAsync(
        StreamId streamId,
        EventId eventId,
        object payload,
        EventMetadata metadata,
        long streamPosition,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default)
    {
        var globalPosition = Interlocked.Increment(ref _currentPosition);
        var record = new EventRecord(
            streamId,
            eventId,
            payload,
            metadata,
            globalPosition,
            streamPosition,
            timestamp);
            
        _events.TryAdd(globalPosition, record);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<Event> FetchAsync(
        StreamPosition fromPosition,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var kvp in _events.Where(e => e.Key >= fromPosition.Value).OrderBy(e => e.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var record = kvp.Value;
            yield return new Event(
                record.StreamId,
                record.EventId,
                StreamPosition.From(record.StreamPosition),
                StreamPosition.From(record.GlobalPosition),
                record.Timestamp,
                BatchId.From(string.Empty),
                record.Payload,
                record.Metadata);
                
            await Task.Yield(); // Allow other tasks to run
        }
    }

    public Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_events.Count > 0 
            ? StreamPosition.From(_events.Max(e => e.Key)) 
            : StreamPosition.Start);
    }

    /// <summary>
    /// Clears all events from the in-memory store
    /// </summary>
    public void Clear()
    {
        _events.Clear();
        _currentPosition = 0;
    }
}