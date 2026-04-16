namespace Streamon.Subscription;

/// <summary>
/// Marker interface for projector type scanning.
/// </summary>
/// <typeparam name="TEvent">The event type this projector handles.</typeparam>
public interface IEventProjector<TEvent>;

/// <summary>
/// Defines a contract for updating existing projection state when an event is received.
/// </summary>
/// <typeparam name="TEvent">The event type that triggers a state update.</typeparam>
/// <typeparam name="TState">The projection state type.</typeparam>
public interface IEventProjector<TEvent, TState>
{
    /// <summary>
    /// Applies the event to existing projection state, returning the updated state.
    /// </summary>
    ValueTask<TState> ProjectAsync(TState state, EventHandlerContext<TEvent> @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a <typeparamref name="TState"/> key template with identity properties populated
    /// from the event context. Used by <see cref="IProjectionStore{TState}.ReadAsync"/> to locate
    /// existing state before applying the update.
    /// </summary>
    TState GetKey(EventHandlerContext<TEvent> @event, CancellationToken cancellationToken = default);
}
