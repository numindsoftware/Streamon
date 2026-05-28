namespace Streamon.Subscription;

public static class EventPipelineBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="InboxDeduplicationMiddleware"/> to the pipeline using the provided
    /// inbox factory. The factory is invoked on each pipeline execution, allowing the caller
    /// to defer inbox resolution (e.g. DI, suffix-aware factories).
    /// </summary>
    /// <param name="builder">The pipeline builder to configure.</param>
    /// <param name="inboxFactory">Factory returning the <see cref="IEventInbox"/> used for deduplication.</param>
    /// <param name="subscriptionId">The subscription identity passed to the inbox.</param>
    /// <param name="consumerName">The consumer name passed to the inbox; typically the subscription id value.</param>
    public static EventPipelineBuilder UseInboxDeduplication(
        this EventPipelineBuilder builder,
        Func<IEventInbox> inboxFactory,
        SubscriptionId subscriptionId,
        string consumerName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(inboxFactory);
        return builder.UseMiddleware(() => new InboxDeduplicationMiddleware(inboxFactory(), subscriptionId, consumerName));
    }
}
