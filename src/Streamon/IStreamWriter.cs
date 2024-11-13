namespace Streamon;

public interface IStreamWriter
{
    Task<Stream> AppendAsync(
        StreamId id,
        StreamPosition expectedPosition, 
        IEnumerable<object> events,
        EventMetadata? metadata = default,
        CancellationToken cancellationToken = default);
}
