namespace Streamon.Subscription;

/// <summary>
/// Controls how a subscription handles exceptions thrown during event dispatch.
/// </summary>
public enum SubscriptionErrorHandling
{
    /// <summary>
    /// Exceptions from handlers and middleware propagate immediately, halting the subscription poll.
    /// </summary>
    Throw,

    /// <summary>
    /// Exceptions from handlers and middleware are silently swallowed, allowing the subscription
    /// to advance its checkpoint and continue processing subsequent events.
    /// </summary>
    Ignore
}
