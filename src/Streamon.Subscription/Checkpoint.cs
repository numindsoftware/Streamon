namespace Streamon.Subscription;

public readonly record struct Checkpoint(
    SubscriptionId SubscriptionId, 
    StreamPosition Position, 
    DateTimeOffset LastModifiedTime = default);