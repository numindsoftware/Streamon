namespace Streamon.Subscription;

/// <summary>
///  using marking interfaces should make it easier to find the handlers and batch them together without explicitly having to implement them
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventAsyncProjector<TEvent>;

public interface IEventAsyncProjector<TEvent, TState>
{
    /// <summary>
    ///     Handles the event asynchronously.
    /// </summary>
    /// <param name="context">The context of the event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask<TState> ProjectAsync(TState state, EventConsumeContext<TEvent> @event, CancellationToken cancellationToken = default);
    string GetIdentity(EventConsumeContext<TEvent> @event, CancellationToken cancellationToken = default);
}
