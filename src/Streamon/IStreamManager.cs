namespace Streamon;

public interface IStreamManager
{
    Task DeleteStreamAsync(
        StreamId streamId,
        StreamPosition expectedSequence,
        CancellationToken cancellationToken = default);
}
