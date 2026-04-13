using Streamon.Subscription;

namespace Streamon.Subscription;

public interface IEventInitialProjector<TEvent, TState>
{
    TState Project(EventHandlerContext<TEvent> @event, CancellationToken cancellationToken = default);
    string GetIdentity(EventHandlerContext<TEvent> @event, CancellationToken cancellationToken = default);
}
