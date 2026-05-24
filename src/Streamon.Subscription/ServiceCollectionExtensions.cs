using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Streamon.Subscription;

public static class ServiceCollectionExtensions
{
    public static StreamSubscriptionBuilder AddStreamSubscription(
        this IServiceCollection services,
        SubscriptionId subscriptionId,
        StreamSubscriptionType streamSubscriptionType = default,
        SubscriptionErrorHandling errorHandling = default,
        EventDispatchType eventDispatchType = default)
    {
        StreamSubscriptionBuilder builder = new(subscriptionId, streamSubscriptionType, errorHandling, eventDispatchType);

        services.TryAddSingleton<IStreamSubscriptionProvisioner, StreamSubscriptionProvisioner>();

        // Builder template — consumed by the provisioner per (SubscriptionId, name).
        // Registered both keyed (for targeted lookup) and non-keyed (so the provisioner
        // can enumerate every registered subscription for eager All() materialization).
        services.AddKeyedSingleton(subscriptionId.Value, (_, _) => builder);
        services.AddSingleton(builder);

        return builder;
    }
}
