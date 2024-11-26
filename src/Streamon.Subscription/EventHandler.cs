namespace Streamon.Subscription;

public abstract class EventHandler : IEventHandler
{
    private readonly Dictionary<Type, Func<EventConsumeContext<object>, CancellationToken, ValueTask>> _handlers = [];

    protected EventHandler On<T>(Func<EventConsumeContext<T>, CancellationToken, ValueTask> handler)
    {
        _handlers.TryAdd(typeof(T), async (context, cancellationToken) =>
        {
            if (context is EventConsumeContext<T> typedContext) await handler(typedContext, cancellationToken);
            else await handler(new EventConsumeContext<T>(context), cancellationToken);
        });
        return this;
    }

    public ValueTask HandleEventAsync(EventConsumeContext<object> context, CancellationToken cancellationToken = default) =>
        _handlers.TryGetValue(context.Payload.GetType(), out var handler) ? handler(context, cancellationToken) : ValueTask.CompletedTask;
}
