namespace Streamon;

public interface IStreamManager
{
    Task<long> DeleteStreamAsync(
        StreamId streamId,
        StreamPosition expectedPosition,
        CancellationToken cancellationToken = default);

    public event EventHandler<StreamIdEventArgs>? StreamDeleted;
}
