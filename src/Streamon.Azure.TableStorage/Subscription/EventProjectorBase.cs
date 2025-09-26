using Streamon.Subscription;
using System.Collections.Concurrent;

namespace Streamon.Azure.TableStorage.Subscription;

public abstract class EventProjectorBase<TProjector, TState> : IEventProjector where TProjector : EventProjectorBase<TProjector, TState>
{
    private static readonly ConcurrentDictionary<Type, Func<TProjector, Event, CancellationToken, ValueTask<TState>>> _initializeHandlers = [];
    private static readonly ConcurrentDictionary<Type, Func<TProjector, TState, Event, CancellationToken, ValueTask<TState>>> _updateHandlers = [];

    static EventProjectorBase()
    {
        var projectorType = typeof(TProjector);

        var initializingEventTypes = projectorType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventAsyncInitialProjector<,>))
            .Select(i => i.GetGenericArguments()[0]);
        foreach (var eventType in initializingEventTypes)
        {
            var eventContextType = typeof(EventConsumeContext<>).MakeGenericType(eventType);
            var method = projectorType.GetMethod(nameof(IEventAsyncInitialProjector<object, object>.ProjectAsync), [ eventContextType, typeof(CancellationToken) ]);
            if (method != null)
            {
                _initializeHandlers.AddOrUpdate(eventType, (TProjector projector, Event @event, CancellationToken cancellationToken) =>
                {
                    var factoryMethod = eventContextType.GetMethod(nameof(EventConsumeContext<object>.From))!;
                    var eventConsumeContext = factoryMethod.Invoke(null, [@event])!;
                    return (ValueTask<TState>)method.Invoke(projector, [eventConsumeContext, cancellationToken])!;
                }, (_, v) => v);
            }
        }

        var eventTypes = projectorType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventAsyncProjector<,>))
            .Select(i => i.GetGenericArguments()[0]);
        foreach (var eventType in eventTypes)
        {
            var eventContextType = typeof(EventConsumeContext<>).MakeGenericType(eventType);
            var method = projectorType.GetMethod(nameof(IEventAsyncProjector<object, object>.ProjectAsync), [typeof(TState), eventContextType, typeof(CancellationToken)]);
            if (method != null)
            {
                _updateHandlers.AddOrUpdate(eventType, (TProjector projector, TState state, Event @event, CancellationToken cancellationToken) =>
                {
                    var factoryMethod = eventContextType.GetMethod(nameof(EventConsumeContext<object>.From))!;
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
