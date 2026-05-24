namespace Streamon.Subscription;

/// <summary>
/// Tracks which events have already been processed by a given consumer to enable
/// idempotent handling under at-least-once delivery semantics.
/// </summary>
/// <remarks>
/// <para>The inbox is the consumer-side counterpart of the transactional outbox pattern.
/// A consumer is uniquely identified by a (<see cref="SubscriptionId"/>, consumer-name) pair —
/// typically the handler's type name — so multiple handlers on the same subscription each
/// maintain an independent processing record.</para>
/// <para><b>Atomicity caveat:</b> unless the handler's side effect writes to the same store
/// as the inbox in a single transaction, the inbox cannot make non-idempotent side effects
/// exactly-once. The recommended order is: run the side effect first (idempotently if
/// possible), then call <see cref="MarkProcessedAsync"/>. On crash between the two, the next
/// delivery re-runs the side effect — which is the safer failure mode for most workloads.</para>
/// </remarks>
public interface IEventInbox
{
    /// <summary>
    /// Returns <see langword="true"/> when the inbox has already recorded
    /// <paramref name="eventId"/> for the consumer identified by
    /// <paramref name="subscriptionId"/> and <paramref name="consumerName"/>.
    /// </summary>
    Task<bool> HasProcessedAsync(
        SubscriptionId subscriptionId,
        string consumerName,
        EventId eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that <paramref name="@event"/> has been processed by the consumer identified
    /// by <paramref name="subscriptionId"/> and <paramref name="consumerName"/>. Idempotent:
    /// re-recording the same event must not throw.
    /// </summary>
    Task MarkProcessedAsync(
        SubscriptionId subscriptionId,
        string consumerName,
        Event @event,
        CancellationToken cancellationToken = default);
}