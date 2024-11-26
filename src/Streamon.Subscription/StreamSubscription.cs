namespace Streamon.Subscription;

internal class StreamSubscription(SubscriptionId subscriptionId, IEventHandlerResolver eventHandlerResolver, ICheckpointStore checkpointStore, ISubscriptionStreamReader subscriptionStreamReader)
{
    private readonly Dictionary<Type, IEventHandler?> _eventHandlers = [];

    public StreamSubscription AddEventHandler<T>() where T : IEventHandler
    {
        _eventHandlers.TryAdd(typeof(T), default);
        return this;
    }

    public virtual async Task SetupAsync(CancellationToken cancellationToken = default)
    {
        //var lastPosition = subscriptionStreamReader.GetLastGlobalPositionAsync(cancellationToken);
        await checkpointStore.SetCheckpointAsync(subscriptionId, StreamPosition.Start, cancellationToken);
    }

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        var lastCheckpoint = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken);
        await foreach (var @event in subscriptionStreamReader.FetchAsync(lastCheckpoint, cancellationToken))
        {
            var context = new EventConsumeContext<object>(subscriptionId, @event.StreamId, @event.EventId, @event.Payload)
            {
                Metadata = @event.Metadata,
                GlobalPosition = @event.GlobalPosition,
                StreamPosition = @event.StreamPosition,
            };

            foreach (var eventHandler in _eventHandlers)
            {
                if (!_eventHandlers.TryGetValue(eventHandler.Key, out var handlerDefinition) && handlerDefinition is null)
                {
                    _eventHandlers[eventHandler.Key] = handlerDefinition = eventHandlerResolver.Resolve(eventHandler.Key);
                }
                await handlerDefinition!.HandleEventAsync(context, cancellationToken);
            }
            await checkpointStore.SetCheckpointAsync(subscriptionId, context.GlobalPosition, cancellationToken);
        }
    }
}
