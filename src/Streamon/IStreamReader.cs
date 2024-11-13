namespace Streamon;

public interface IStreamReader
{
    Task<Stream> FetchAsync(
        StreamId streamId, 
        StreamPosition startPosition = default, 
        StreamPosition endPosition = default, 
        CancellationToken cancellationToken = default);
}