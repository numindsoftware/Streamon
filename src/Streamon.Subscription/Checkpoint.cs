namespace Streamon.Subscription;

public record Checkpoint(string SubscriptionId, long Position, DateTimeOffset LastModifiedTime = default);