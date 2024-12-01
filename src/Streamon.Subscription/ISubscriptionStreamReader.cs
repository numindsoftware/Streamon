namespace Streamon.Subscription;

public interface ISubscriptionStreamReader
{
    public IAsyncEnumerable<Event> FetchAsync(StreamPosition fromPosition, CancellationToken cancellationToken = default);
    public Task<StreamPosition> GetLastGlobalPositionAsync(CancellationToken cancellationToken = default);
}
