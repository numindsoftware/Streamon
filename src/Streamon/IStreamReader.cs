namespace Streamon;

public interface IStreamReader
{
    Task<IEnumerable<Event>> FetchEventsAsync(
        StreamId streamId, 
        StreamPosition startPosition = default, 
        StreamPosition endPosition = default,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}