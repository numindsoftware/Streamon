using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class StreamSubscription(
    SubscriptionId subscriptionId, 
    StreamSubscriptionType streamSubscriptionType, 
    ICheckpointStore checkpointStore, 
    ISubscriptionStreamReader subscriptionStreamReader,
    IEventHandlerRegistry eventHandlerRegistry,
    IServiceProvider serviceProvider)
{
    public StreamSubscription AddEventHandler<THandler>()
    {
        eventHandlerRegistry.RegisterHandlersFrom(typeof(THandler));
        return this;
    }

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
            //var context = new EventConsumeContext<object>(subscriptionId, @event.StreamId, @event.EventId, @event.Payload)
            //{
            //    Metadata = @event.Metadata,
            //    GlobalPosition = @event.GlobalPosition,
            //    StreamPosition = @event.StreamPosition,
            //    Timestamp = @event.Timestamp
            //};
            var globalPosition = @event.GlobalPosition;

            try
            {
                foreach (var eventHandler in eventHandlerRegistry.GetHandlers(@event.Payload.GetType()))
                {
                    var handlerInstance = serviceProvider.GetService(eventHandler.HandlerType) ?? ActivatorUtilities.CreateInstance(serviceProvider, eventHandler.HandlerType);
                }
                globalPosition = await subscriptionStreamReader.GetLastGlobalPositionAsync(cancellationToken);
            }
            finally
            {
                await checkpointStore.SetCheckpointAsync(subscriptionId, globalPosition, cancellationToken);
            }
        }
    }
}
