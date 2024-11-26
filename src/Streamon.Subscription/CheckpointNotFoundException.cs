namespace Streamon.Subscription;

[Serializable]
public class CheckpointNotFoundException(SubscriptionId subscriptionId, string? message = default, Exception? innerException = default) : Exception(message, innerException)
{
    public SubscriptionId SubscriptionId { get; } = subscriptionId;
}
