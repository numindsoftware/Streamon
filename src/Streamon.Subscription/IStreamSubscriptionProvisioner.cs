namespace Streamon.Subscription;

/// <summary>
/// Creates and caches <see cref="StreamSubscription"/> instances on demand. Each
/// (<see cref="SubscriptionId"/>, name) pair maps to a single cached subscription whose
/// component names are composed from the registration-time prefixes and the
/// provisioner-supplied suffix via the configured naming convention.
/// </summary>
/// <remarks>
/// The provisioner is intentionally not a poller. To drive subscriptions, enumerate them
/// via <see cref="All"/> (or fetch a specific one via <see cref="Get"/>) and call
/// <see cref="StreamSubscription.PollAsync"/> directly.
/// </remarks>
public interface IStreamSubscriptionProvisioner
{
    /// <summary>Returns (creating if necessary) the subscription for the given identity and name.</summary>
    Task<StreamSubscription> CreateSubscriptionAsync(
        SubscriptionId subscriptionId,
        string name = "",
        CancellationToken cancellationToken = default);

    /// <summary>Enumerates every subscription and creates them if necessary. Useful when not knowing the subscription ids in advance.</summary>
    IEnumerable<StreamSubscription> All(string name = "");
}