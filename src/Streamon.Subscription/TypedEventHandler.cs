using System.Reflection;

namespace Streamon.Subscription;

/// <summary>
/// An <see cref="IEventHandler"/> that composes strongly-typed <see cref="IEventHandler{TEvent}"/>
/// handlers, dispatching events by payload type and delivering a typed <see cref="EventHandlerContext{T}"/>.
/// </summary>
public class TypedEventHandler(SubscriptionId subscriptionId) : IEventHandler
{
    private readonly Dictionary<Type, List<Func<Event, CancellationToken, Task>>> _dispatchers = [];

    /// <summary>
    /// Registers a strongly-typed event handler for <typeparamref name="TEvent"/>.
    /// </summary>
    public TypedEventHandler RegisterHandler<TEvent>(IEventHandler<TEvent> handler)
    {
        var eventType = typeof(TEvent);
        if (!_dispatchers.TryGetValue(eventType, out var list))
        {
            list = [];
            _dispatchers[eventType] = list;
        }
        list.Add((e, ct) => handler.HandleAsync(EventHandlerContext<TEvent>.From(subscriptionId, e), ct));
        return this;
    }

    /// <summary>
    /// Scans <paramref name="handlerInstance"/> for all <see cref="IEventHandler{TEvent}"/> implementations
    /// and registers each one via <see cref="RegisterHandler{TEvent}"/>.
    /// </summary>
    public void RegisterHandlersFrom(object handlerInstance)
    {
        var handlerInterfaces = handlerInstance.GetType()
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

        foreach (var iface in handlerInterfaces)
        {
            var eventType = iface.GetGenericArguments()[0];
            typeof(TypedEventHandler)
                .GetMethod(nameof(RegisterHandler), BindingFlags.Instance | BindingFlags.Public)!
                .MakeGenericMethod(eventType)
                .Invoke(this, [handlerInstance]);
        }
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        if (_dispatchers.TryGetValue(@event.Payload.GetType(), out var dispatchers))
        {
            foreach (var dispatcher in dispatchers)
            {
                await dispatcher(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}