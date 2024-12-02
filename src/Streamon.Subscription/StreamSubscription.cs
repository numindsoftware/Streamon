namespace Streamon.Subscription;

public class StreamSubscription(SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType, IEventHandlerResolver eventHandlerResolver, ICheckpointStore checkpointStore, ISubscriptionStreamReader subscriptionStreamReader)
{
    private readonly Dictionary<Type, IEventHandler?> _eventHandlers = [];

    public StreamSubscription AddEventHandler(Type eventHandlerType)
    {
        if (!eventHandlerType.IsAssignableTo(typeof(IEventHandler))) throw new EventHandlerRegistrationException(eventHandlerType, "Event handlers from implement or inherit from a type which implements the IEventHandler interface");
        _eventHandlers.TryAdd(eventHandlerType, default);
        return this;
    }

    public StreamSubscription AddEventHandler<T>() where T : class, IEventHandler => AddEventHandler(typeof(T));

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        var lastCheckpoint = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken);
        if (lastCheckpoint == StreamPosition.End)
        {
            if (streamSubscriptionType == StreamSubscriptionType.CatchUp)
            {
                await checkpointStore.SetCheckpointAsync(subscriptionId, StreamPosition.Start, cancellationToken);
                lastCheckpoint = StreamPosition.Start;
            }
            else
            {
                var lastPosition = await subscriptionStreamReader.GetLastGlobalPositionAsync(cancellationToken);
                await checkpointStore.SetCheckpointAsync(subscriptionId, lastPosition, cancellationToken);
                lastCheckpoint = lastPosition;
            }
        }

        await foreach (var @event in subscriptionStreamReader.FetchAsync(lastCheckpoint, cancellationToken))
        {
            var context = new EventConsumeContext<object>(subscriptionId, @event.StreamId, @event.EventId, @event.Payload)
            {
                Metadata = @event.Metadata,
                GlobalPosition = @event.GlobalPosition,
                StreamPosition = @event.StreamPosition,
                Timestamp = @event.Timestamp
            };
            foreach (var eventHandler in _eventHandlers)
            {
                var handler = eventHandler.Value;
                if (handler is null) _eventHandlers[eventHandler.Key] = handler = eventHandlerResolver.Resolve(eventHandler.Key);
                await handler!.HandleAsync(context, cancellationToken);
            }
            await checkpointStore.SetCheckpointAsync(subscriptionId, context.GlobalPosition, cancellationToken);
        }
    }
}
