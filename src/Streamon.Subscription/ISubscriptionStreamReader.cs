using System.Runtime.CompilerServices;

namespace Streamon.Subscription;

public interface ISubscriptionStreamReader
{
    public IAsyncEnumerable<EventEnvelope> FetchAsync(Checkpoint fromCheckpoint, CancellationToken cancellationToken = default);
}
