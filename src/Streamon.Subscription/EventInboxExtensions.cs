namespace Streamon.Subscription;

public static class EventInboxExtensions
{
    /// <summary>
    /// Runs <paramref name="handler"/> only if <paramref name="@event"/> has not yet been
    /// recorded in the inbox for the given consumer; marks it processed on success.
    /// </summary>
    /// <remarks>
    /// Side-effect-first ordering: the handler runs before the inbox is updated, so a crash
    /// in between causes a redelivery (preferred over silent drop). The handler should be
    /// idempotent whenever possible.
    /// </remarks>
    public static async ValueTask RunOnceAsync(
        this IEventInbox inbox,
        SubscriptionId subscriptionId,
        string consumerName,
        Event @event,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken = default)
    {
        if (await inbox.HasProcessedAsync(subscriptionId, consumerName, @event.EventId, cancellationToken).ConfigureAwait(false))
            return;

        await handler(cancellationToken).ConfigureAwait(false);
        await inbox.MarkProcessedAsync(subscriptionId, consumerName, @event, cancellationToken).ConfigureAwait(false);
    }
}