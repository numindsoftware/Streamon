using System.Collections.Concurrent;

namespace Streamon.Subscription;

///// <summary>
///// An in-memory subscription that supports both sequential and parallel event processing
///// </summary>
//public class InMemoryStreamSubscription(
//        SubscriptionId subscriptionId,
//        ICheckpointStore checkpointStore,
//        InMemorySubscriptionStreamReader streamReader,
//        IEventHandlerRegistry eventHandlerRegistry,
//        DispatchMode dispatchMode = DispatchMode.Sequential)
//{
//    private readonly ConcurrentQueue<Event> _pendingEvents = new();
//    private int _isProcessing;

//    /// <summary>
//    /// Adds an event handler to this subscription
//    /// </summary>
//    public InMemoryStreamSubscription AddEventHandler<T>()
//    {
//        eventHandlerRegistry.RegisterHandlersFrom(typeof(T));
//        return this;
//    }

//    /// <summary>
//    /// Publishes an event to the in-memory stream and processes it according to the dispatch mode
//    /// </summary>
//    public async Task PublishEventAsync(
//        StreamId streamId,
//        EventId eventId,
//        object payload,
//        EventMetadata metadata,
//        long streamPosition,
//        DateTimeOffset timestamp,
//        CancellationToken cancellationToken = default)
//    {
//        await streamReader.PublishEventAsync(
//            streamId, 
//            eventId, 
//            payload, 
//            metadata, 
//            streamPosition, 
//            timestamp, 
//            cancellationToken);
            
//        await ProcessPendingEventsAsync(cancellationToken);
//    }

//    /// <summary>
//    /// Processes all pending events that have been published
//    /// </summary>
//    public async Task ProcessPendingEventsAsync(CancellationToken cancellationToken = default)
//    {
//        // Prevent concurrent processing
//        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0) 
//            return;

//        try
//        {
//            var lastCheckpoint = await checkpointStore.GetCheckpointAsync(subscriptionId, cancellationToken);
//            if (lastCheckpoint == StreamPosition.End)
//            {
//                var lastPosition = await streamReader.GetLastGlobalPositionAsync(cancellationToken);
//                await checkpointStore.SetCheckpointAsync(subscriptionId, StreamPosition.Start, cancellationToken);
//                lastCheckpoint = StreamPosition.Start;
//            }

//            var events = new List<Event>();
//            await foreach (var @event in streamReader.FetchAsync(lastCheckpoint, cancellationToken))
//            {
//                events.Add(@event);
//            }

//            if (events.Count == 0)
//                return;

//            if (dispatchMode == DispatchMode.Sequential)
//            {
//                await ProcessEventsSequentiallyAsync(events, cancellationToken);
//            }
//            else
//            {
//                await ProcessEventsInParallelAsync(events, cancellationToken);
//            }
//        }
//        finally
//        {
//            Interlocked.Exchange(ref _isProcessing, 0);
//        }
//    }

//    private async Task ProcessEventsSequentiallyAsync(List<Event> events, CancellationToken cancellationToken)
//    {
//        foreach (var @event in events)
//        {
//            var context = CreateConsumeContext(@event);
            
//            foreach (var eventHandler in _eventHandlers)
//            {
//                var handler = eventHandler.Value;
//                if (handler is null) 
//                    _eventHandlers[eventHandler.Key] = handler = _eventHandlerResolver.Resolve(eventHandler.Key);
                
//                await handler!.HandleAsync(context, cancellationToken);
//            }
            
//            await checkpointStore.SetCheckpointAsync(subscriptionId, context.GlobalPosition, cancellationToken);
//        }
//    }

//    private async Task ProcessEventsInParallelAsync(List<Event> events, CancellationToken cancellationToken)
//    {
//        var tasks = new List<Task>();
//        var highestPosition = 0L;
        
//        foreach (var @event in events)
//        {
//            var context = CreateConsumeContext(@event);
//            highestPosition = Math.Max(highestPosition, context.GlobalPosition.Value);
            
//            foreach (var eventHandler in _eventHandlers)
//            {
//                var handler = eventHandler.Value;
//                if (handler is null) 
//                    eventHandlers[eventHandler.Key] = handler = _eventHandlerResolver.Resolve(eventHandler.Key);
                
//                tasks.Add(handler!.HandleAsync(context, cancellationToken).AsTask());
//            }
//        }
        
//        await Task.WhenAll(tasks);
        
//        // Update checkpoint after all events have been processed
//        if (highestPosition > 0)
//        {
//            await checkpointStore.SetCheckpointAsync(subscriptionId, StreamPosition.From(highestPosition), cancellationToken);
//        }
//    }
//}