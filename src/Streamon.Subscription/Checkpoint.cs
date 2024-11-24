namespace Streamon.Subscription;

public readonly record struct Checkpoint(string SubscriptionId, long Position, DateTimeOffset LastModifiedTime = default);