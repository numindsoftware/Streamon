using Streamon.Subscription;

namespace Streamon.Subscription;

/// <summary>
/// Defines a contract for creating initial projection state from an event. The returned state
/// must have all key properties populated so that the <see cref="IProjectionStore{TState}"/>
/// can derive storage-specific identifiers from it.
/// </summary>
/// <typeparam name="TEvent">The event type that triggers initial state creation.</typeparam>
/// <typeparam name="TState">The projection state type.</typeparam>
public interface IEventInitialProjector<TEvent, TState>
{
    TState Project(EventHandlerContext<TEvent> @event, CancellationToken cancellationToken = default);
}
