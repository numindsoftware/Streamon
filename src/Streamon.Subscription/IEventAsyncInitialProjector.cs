using Streamon.Subscription;

namespace Streamon.Subscription;

public interface IEventAsyncInitialProjector<TEvent, TState>
{
    ValueTask<TState> ProjectAsync(EventConsumeContext<TEvent> @event, CancellationToken cancellationToken = default);
    string GetIdentity(EventConsumeContext<TEvent> @event, CancellationToken cancellationToken = default);
}
