using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Streamon.Subscription;

/// <summary>
/// Provides a thread-safe registry for managing event handler delegates, enabling dynamic registration and retrieval of
/// handlers for specific event types.
/// </summary>
/// <remarks>The EventHandlerRegistry class implements the IEventHandlerRegistry interface and supports the
/// registration of handlers from types that implement the IEventHandler<T> interface. Handlers can be registered at
/// runtime and are efficiently retrieved based on the event type. This class is designed for use in event-driven
/// architectures where decoupled event handling is required. All operations are thread-safe, making the registry
/// suitable for concurrent scenarios.</remarks>
public class EventHandlerRegistry : IEventHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, Dictionary<Type, Func<object, object, CancellationToken, Task>>> _handlers = [];

    public void RegisterHandlersFrom(Type handlersType)
    {
        var handlerInterfaces = handlersType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));
        foreach (var handlerInterface in handlerInterfaces)
        {
            var eventType = handlerInterface.GetGenericArguments()[0];
            var handleMethod = handlerInterface.GetMethod(nameof(IEventHandler<object>.HandleAsync), BindingFlags.Instance | BindingFlags.Public);
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
        var eventContextType = typeof(EventHandlerContext<>).MakeGenericType(eventType);

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
