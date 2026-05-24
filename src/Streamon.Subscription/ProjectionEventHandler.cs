using System.Reflection;

namespace Streamon.Subscription;

/// <summary>
/// An <see cref="IEventHandler"/> that dispatches events to registered
/// <see cref="IEventInitialProjector{TEvent, TState}"/> and <see cref="IEventProjector{TEvent, TState}"/>
/// implementations, persisting projection state via an <see cref="IProjectionStore{TState}"/>.
/// </summary>
/// <remarks>
/// Each event payload type may have at most one dispatcher (initial or update). If both
/// <see cref="IEventInitialProjector{TEvent, TState}"/> and <see cref="IEventProjector{TEvent, TState}"/>
/// are implemented for the same event type on a single projector, the update projector takes precedence.
/// Events whose payload type has no registered projector are silently skipped.
/// </remarks>
/// <typeparam name="TState">The projection state type.</typeparam>
public class ProjectionEventHandler<TState>(
    SubscriptionId subscriptionId,
    IProjectionStore<TState> projectionStore) : IEventHandler
{
    private readonly Dictionary<Type, Func<Event, CancellationToken, Task>> _dispatchers = [];

    /// <summary>
    /// Registers an <see cref="IEventInitialProjector{TEvent, TState}"/> that creates projection state
    /// when an event of type <typeparamref name="TEvent"/> is received. The produced state is written
    /// (upserted) to the store regardless of whether state already exists for the key.
    /// </summary>
    public ProjectionEventHandler<TState> RegisterInitialProjector<TEvent>(
        IEventInitialProjector<TEvent, TState> projector)
    {
        _dispatchers[typeof(TEvent)] = async (@event, ct) =>
        {
            var context = EventHandlerContext<TEvent>.From(subscriptionId, @event);
            var state = projector.Project(context, ct);
            StampTrackingPosition(state, @event);
            await projectionStore.WriteAsync(state, ct).ConfigureAwait(false);
        };
        return this;
    }

    /// <summary>
    /// Registers an <see cref="IEventProjector{TEvent, TState}"/> that updates existing projection state
    /// when an event of type <typeparamref name="TEvent"/> is received. If no state exists for the
    /// key returned by <see cref="IEventProjector{TEvent, TState}.GetKey"/>, the event is skipped.
    /// </summary>
    public ProjectionEventHandler<TState> RegisterProjector<TEvent>(
        IEventProjector<TEvent, TState> projector)
    {
        _dispatchers[typeof(TEvent)] = async (@event, ct) =>
        {
            var context = EventHandlerContext<TEvent>.From(subscriptionId, @event);
            var keyState = projector.GetKey(context, ct);
            var existingState = await projectionStore.ReadAsync(keyState, ct).ConfigureAwait(false);
            if (existingState is null) return;
            if (HasAlreadyApplied(existingState, @event)) return;
            var updatedState = await projector.ProjectAsync(existingState, context, ct).ConfigureAwait(false);
            StampTrackingPosition(updatedState, @event);
            await projectionStore.WriteAsync(updatedState, ct).ConfigureAwait(false);
        };
        return this;
    }

    private static void StampTrackingPosition(TState state, Event @event)
    {
        if (state is IProjectionTrackable trackable)
        {
            trackable.ProjectionTrackingPosition = @event.GlobalPosition.Value;
        }
    }

    private static bool HasAlreadyApplied(TState state, Event @event) =>
        state is IProjectionTrackable trackable
            && trackable.ProjectionTrackingPosition >= @event.GlobalPosition.Value;

    /// <summary>
    /// Scans <paramref name="projectorInstance"/> for all <see cref="IEventInitialProjector{TEvent, TState}"/>
    /// and <see cref="IEventProjector{TEvent, TState}"/> implementations and registers each one.
    /// Initial projectors are registered first; update projectors registered second will overwrite
    /// an initial dispatcher for the same event type.
    /// </summary>
    public void RegisterProjectorsFrom(object projectorInstance)
    {
        foreach (var iface in GetProjectorInterfaces(projectorInstance.GetType(), typeof(IEventInitialProjector<,>)))
        {
            var eventType = iface.GetGenericArguments()[0];
            typeof(ProjectionEventHandler<TState>)
                .GetMethod(nameof(RegisterInitialProjector), BindingFlags.Instance | BindingFlags.Public)!
                .MakeGenericMethod(eventType)
                .Invoke(this, [projectorInstance]);
        }

        foreach (var iface in GetProjectorInterfaces(projectorInstance.GetType(), typeof(IEventProjector<,>)))
        {
            var eventType = iface.GetGenericArguments()[0];
            typeof(ProjectionEventHandler<TState>)
                .GetMethod(nameof(RegisterProjector), BindingFlags.Instance | BindingFlags.Public)!
                .MakeGenericMethod(eventType)
                .Invoke(this, [projectorInstance]);
        }
    }

    /// <inheritdoc/>
    public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
    {
        if (_dispatchers.TryGetValue(@event.Payload.GetType(), out var dispatcher))
        {
            await dispatcher(@event, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IEnumerable<Type> GetProjectorInterfaces(Type type, Type genericDefinition) =>
        type.GetInterfaces()
            .Where(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == genericDefinition
                && i.GetGenericArguments()[1] == typeof(TState));
}