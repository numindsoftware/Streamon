namespace Streamon.Subscription;

/// <summary>
/// Defines the contract for reading and writing projection state. Storage-specific keys are
/// derived from the <typeparamref name="TState"/> instance by the store implementation.
/// </summary>
/// <typeparam name="TState">The projection state type managed by this store.</typeparam>
public interface IProjectionStore<TState>
{
    /// <summary>
    /// Reads the projection state matching the key properties of <paramref name="keyState"/>.
    /// Returns <c>null</c> if no state exists for the derived key.
    /// </summary>
    Task<TState?> ReadAsync(TState keyState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces the projection state. Storage keys are derived from
    /// <paramref name="state"/> by the store implementation.
    /// </summary>
    Task WriteAsync(TState state, CancellationToken cancellationToken = default);
}