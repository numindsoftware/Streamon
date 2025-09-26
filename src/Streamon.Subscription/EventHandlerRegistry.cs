using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Streamon.Subscription;

public class EventHandlerRegistry : IEventHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, Dictionary<Type, Func<object, object, CancellationToken, Task>>> _handlers = [];

    public void RegisterHandlersFrom(Type handlersType)
    {
        var handlerInterfaces = handlersType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventAsyncHandler<>));
        foreach (var handlerInterface in handlerInterfaces)
        {
            var eventType = handlerInterface.GetGenericArguments()[0];
            var handleMethod = handlerInterface.GetMethod(nameof(IEventAsyncHandler<object>.HandleAsync), BindingFlags.Instance | BindingFlags.Public);
            if (handleMethod == null) continue;
            AddHandler(handlerInterface, handleMethod, eventType);
        }
    }

    /// <summary>
    /// Gets all handler delegates for a given event type.
    /// </summary>
    public IReadOnlyCollection<EventHandlersEntry> GetHandlers(Type eventType) =>
        _handlers.TryGetValue(eventType, out var list) && list is not null
            ? [.. list.Select(kvp => new EventHandlersEntry(kvp.Key, kvp.Value))]
            : [];

    private void AddHandler(Type handlerType, MethodInfo handleMethod, Type eventType)
    {
        var typeHandlers = _handlers.GetOrAdd(handlerType, _ => []);
        var eventContextType = typeof(EventConsumeContext<>).MakeGenericType(eventType);

        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var eventContextParam = Expression.Parameter(typeof(object), "context");
        var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
        var castInstance = Expression.Convert(instanceParam, handlerType);
        var castEventContext = Expression.Convert(eventContextParam, eventContextType);
        var callHandle = Expression.Call(castInstance, handleMethod, castEventContext, cancellationTokenParam);
        var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task>>(callHandle, instanceParam, eventContextParam, cancellationTokenParam).Compile();

        typeHandlers.TryAdd(eventType, lambda);
    }
}
