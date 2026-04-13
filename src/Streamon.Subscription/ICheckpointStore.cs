namespace Streamon.Subscription;

/// <summary>
/// Defines a contract for persisting and retrieving checkpoints for streaming subscriptions, enabling reliable tracking
/// of event processing progress.
/// </summary>
/// <remarks>Implementations of this interface should ensure thread safety and support for cancellation.
/// Persisting checkpoints allows subscriptions to resume event processing from a known position after interruptions,
/// minimizing the risk of event loss or duplication. The checkpoint store is typically used in event-driven or
/// streaming systems to maintain processing state across restarts or failures.</remarks>
public interface ICheckpointStore
{
    /// <summary>
    /// Given a subscription id, retrieve the last saved checkpoint stream position.
    /// If no checkpoint is found, a StreamPosition.End is returned, this guarantees no events will be returned or processed and avoids exception cases.
    /// </summary>
    public Task<StreamPosition> GetCheckpointAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Asynchronously sets a checkpoint for the specified subscription at the given stream position.
    /// </summary>
    /// <remarks>Use this method to persist the progress of a subscription, allowing event processing to
    /// resume from the specified position in case of interruptions. The operation may take time to complete depending
    /// on the underlying storage implementation. Ensure to handle cancellation appropriately by monitoring the provided
    /// cancellation token.</remarks>
    /// <param name="subscriptionId">The unique identifier of the subscription for which the checkpoint is being set.</param>
    /// <param name="position">The position in the stream that represents the last successfully processed event.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation of setting the checkpoint.</returns>
    public Task SetCheckpointAsync(SubscriptionId subscriptionId, StreamPosition position, CancellationToken cancellationToken = default);
}
