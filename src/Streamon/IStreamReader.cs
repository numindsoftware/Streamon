namespace Streamon;

public interface IStreamReader
{
    Task<IEnumerable<Event>> FetchEventsAsync(
        StreamId streamId, 
        StreamPosition startPosition = default, 
        StreamPosition endPosition = default, 
        CancellationToken cancellationToken = default);
}