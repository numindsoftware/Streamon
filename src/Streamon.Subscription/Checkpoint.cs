namespace Streamon.Subscription;

public readonly record struct Checkpoint(SubscriptionId SubscriptionId, long Position, DateTimeOffset LastModifiedTime = default);