using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Streamon.Subscription;

public static class ServiceCollectionExtensions
{
    public static StreamSubscriptionBuilder AddStreamonSubscription(
        this IServiceCollection services,
        SubscriptionId subscriptionId,
        Action<StreamSubscriptionOptions>? configureOptions = default)
    {
        StreamSubscriptionOptions options = new();
        configureOptions?.Invoke(options);

        StreamSubscriptionBuilder builder = new(subscriptionId, options);
        services.TryAddSingleton<IStreamSubscriptionProvisioner, StreamSubscriptionProvisioner>();

        // Builder template — consumed by the provisioner per (SubscriptionId, name).
        // Registered both keyed (for targeted lookup) and non-keyed (so the provisioner
        // can enumerate every registered subscription for eager All() materialization).
        services.AddKeyedSingleton(subscriptionId.Value, builder);
        services.AddSingleton(builder);

        return builder;
    }
}
