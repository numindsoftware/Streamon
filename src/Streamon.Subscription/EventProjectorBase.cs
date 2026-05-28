using System.Collections.Concurrent;

namespace Streamon.Subscription;

public abstract class EventProjectorBase<TProjector, TState> where TProjector : EventProjectorBase<TProjector, TState>
{
    private static readonly ConcurrentDictionary<Type, Func<TProjector, Event, CancellationToken, ValueTask<TState>>> _initializeHandlers = [];
    private static readonly ConcurrentDictionary<Type, Func<TProjector, TState, Event, CancellationToken, ValueTask<TState>>> _updateHandlers = [];

    static EventProjectorBase()
    {
        var projectorType = typeof(TProjector);

        var initializingEventTypes = projectorType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventInitialProjector<,>))
            .Select(i => i.GetGenericArguments()[0]);
        foreach (var eventType in initializingEventTypes)
        {
            var eventContextType = typeof(EventHandlerContext<>).MakeGenericType(eventType);
            var method = projectorType.GetMethod(nameof(IEventInitialProjector<object, object>.Project), [ eventContextType, typeof(CancellationToken) ]);
            if (method != null)
            {
                _initializeHandlers.AddOrUpdate(eventType, (projector, @event, cancellationToken) =>
                {
                    var factoryMethod = eventContextType.GetMethod(nameof(EventHandlerContext<object>.From))!;
                    var eventConsumeContext = factoryMethod.Invoke(null, [@event])!;
                    return (ValueTask<TState>)method.Invoke(projector, [eventConsumeContext, cancellationToken])!;
                }, (_, v) => v);
            }
        }

        var eventTypes = projectorType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventProjector<,>))
            .Select(i => i.GetGenericArguments()[0]);
        foreach (var eventType in eventTypes)
        {
            var eventContextType = typeof(EventHandlerContext<>).MakeGenericType(eventType);
            var method = projectorType.GetMethod(nameof(IEventProjector<object, object>.ProjectAsync), [typeof(TState), eventContextType, typeof(CancellationToken)]);
            if (method != null)
            {
                _updateHandlers.AddOrUpdate(eventType, (projector, state, @event, cancellationToken) =>
                {
                    var factoryMethod = eventContextType.GetMethod(nameof(EventHandlerContext<object>.From))!;
                    var eventConsumeContext = factoryMethod.Invoke(null, [@event])!;
                    return (ValueTask<TState>)method.Invoke(projector, [state, eventConsumeContext, cancellationToken])!;
                }, (_, v) => v);
            }
        }
    }

    public Task ProjectAsync(Event @event, CancellationToken cancellationToken = default)
    {
        //var eventType = @event.Payload.GetType();
        //if (_initializeHandlers.TryGetValue(eventType, out Func<TProjector, Event, CancellationToken, ValueTask<TState>>? initializeHandler))
        //{
        //    var state = await initializeHandler((TProjector)this, @event, cancellationToken);
        //    //await SaveState(state);
        //}
        //else if(_updateHandlers.TryGetValue(eventType, out Func<TProjector, TState, Event, CancellationToken, ValueTask<TState>>? updateHandler))
        //{
        //    //var state = await ReadStateAsync();
        //    await updateHandler((TProjector)this, state, @event, cancellationToken);
        //    //await SaveState(state);
        //}
        throw new NotImplementedException();
    }
}
