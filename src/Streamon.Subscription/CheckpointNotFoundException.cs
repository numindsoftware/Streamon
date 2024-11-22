namespace Streamon.Subscription;

public class CheckpointNotFoundException(string subscriptionId, string? message = default, Exception? innerException = default) : Exception(message, innerException)
{
    public string SubscriptionId { get; } = subscriptionId;
}
