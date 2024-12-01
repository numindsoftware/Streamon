namespace Streamon.Subscription;

public interface ICheckpointStore
{
    /// <summary>
    /// Given a subscription id, retrieve the last saved checkpoint stream position.
    /// If no checkpoint is found, a StreamPosition.End is returned, this guarantees no events will be returned or processed and avoids exception cases.
    /// </summary>
    public Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default);
    public Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default);
}
