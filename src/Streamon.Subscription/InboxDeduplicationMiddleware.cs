namespace Streamon.Subscription;

/// <summary>
/// Pipeline middleware that uses an <see cref="IEventInbox"/> to make handler dispatch idempotent
/// under at-least-once delivery semantics. For each incoming event the middleware:
/// <list type="number">
/// <item><description>queries <see cref="IEventInbox.HasProcessedAsync"/> and short-circuits the pipeline
/// when the event has already been recorded for the (<see cref="SubscriptionId"/>, <c>consumerName</c>) pair;</description></item>
/// <item><description>invokes the rest of the pipeline (<c>next</c>) otherwise;</description></item>
/// <item><description>calls <see cref="IEventInbox.MarkProcessedAsync"/> on successful completion of <c>next</c>.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Side-effect-first ordering: the handler runs before the inbox is updated, so a crash between
/// <c>next</c> and <c>MarkProcessedAsync</c> causes a redelivery on the next poll — preferred over a
/// silent drop. Handlers should be idempotent whenever possible. This is the dispatch-pipeline
/// equivalent of the consumer-managed <see cref="EventInboxExtensions.RunOnceAsync"/> extension.
/// </remarks>
public sealed class InboxDeduplicationMiddleware(
    IEventInbox inbox,
    SubscriptionId subscriptionId,
    string consumerName) : IEventMiddleware
{
    private readonly IEventInbox _inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
    private readonly string _consumerName = string.IsNullOrEmpty(consumerName)
        ? throw new ArgumentException("Consumer name must be provided.", nameof(consumerName))
        : consumerName;

    public async Task InvokeAsync(Event @event, EventHandlerDelegate next, CancellationToken cancellationToken = default)
    {
        if (await _inbox.HasProcessedAsync(subscriptionId, _consumerName, @event.EventId, cancellationToken).ConfigureAwait(false))
            return;

        await next(@event, cancellationToken).ConfigureAwait(false);
        await _inbox.MarkProcessedAsync(subscriptionId, _consumerName, @event, cancellationToken).ConfigureAwait(false);
    }
}
