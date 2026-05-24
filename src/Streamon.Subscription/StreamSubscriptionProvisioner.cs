using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

internal sealed class StreamSubscriptionProvisioner(
    IServiceProvider serviceProvider,
    IEnumerable<StreamSubscriptionBuilder> registeredBuilders) : IStreamSubscriptionProvisioner
{
    private readonly ConcurrentDictionary<(string SubscriptionId, string Name), StreamSubscription> _cache = new();

    public Task<StreamSubscription> CreateSubscriptionAsync(
        SubscriptionId subscriptionId,
        string name = "",
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_cache.GetOrAdd((subscriptionId.Value, name), BuildSubscription));

    public IEnumerable<StreamSubscription> All(string name = "")
    {
        // Yield every registered subscription under its default (empty) name, building lazily
        // on first access so callers don't have to call CreateSubscriptionAsync explicitly.
        foreach (var builder in registeredBuilders)
        {
            yield return _cache.GetOrAdd((builder.SubscriptionId.Value, name), BuildSubscription);
        }
    }

    private StreamSubscription BuildSubscription((string SubscriptionId, string Name) key)
    {
        var builder = serviceProvider.GetRequiredKeyedService<StreamSubscriptionBuilder>(key.SubscriptionId);
        builder.WithServiceResolver(type =>
            serviceProvider.GetService(type) ?? ActivatorUtilities.CreateInstance(serviceProvider, type));
        return builder.Build(key.Name);
    }
}
