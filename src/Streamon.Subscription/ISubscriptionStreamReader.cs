using System.Runtime.CompilerServices;

namespace Streamon.Subscription;

public interface ISubscriptionStreamReader
{
    public IAsyncEnumerable<Event> FetchAsync(Checkpoint fromCheckpoint, CancellationToken cancellationToken = default);
}
