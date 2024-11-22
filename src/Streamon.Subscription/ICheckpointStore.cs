namespace Streamon.Subscription;

public interface ICheckpointStore
{
    public Task<Checkpoint> GetCheckpointAsync(string subscriptionId);
    public Task SetCheckpointAsync(Checkpoint checkpoint);
}
