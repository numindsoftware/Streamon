using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Streamon.Subscription;

public static class ServiceCollectionExtensions
{
    public static StreamSubscriptionBuilder AddStreamSubscription(this IServiceCollection services, SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType = default, SubscriptionErrorHandling subscriptionErrorHandling = default, EventDispatchType eventDispatchType = default)
    {
        StreamSubscriptionBuilder streamSubscriptionBuilder = new(subscriptionId, streamSubscriptionType, subscriptionErrorHandling, eventDispatchType);
        services.TryAddSingleton<SubscriptionManager>();
        services.AddKeyedSingleton(subscriptionId.Value, (sp, _) =>
        {
            // Bridge IServiceProvider → service resolver at resolution time
            streamSubscriptionBuilder.WithServiceResolver(type =>
                sp.GetService(type) ?? ActivatorUtilities.CreateInstance(sp, type));
            return streamSubscriptionBuilder.Build();
        });
        return streamSubscriptionBuilder;
    }
}
