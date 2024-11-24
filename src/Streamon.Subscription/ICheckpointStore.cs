namespace Streamon.Subscription;

public interface ICheckpointStore
{
    public Task<Checkpoint> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default);
    public Task SetCheckpointAsync(Checkpoint checkpoint, CancellationToken cancellationToken = default);
}
