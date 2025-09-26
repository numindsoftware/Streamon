using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Streamon.Subscription;

public static class ServiceCollectionExtensions
{
    public static StreamSubscriptionBuilder AddStreamSubscription(this IServiceCollection services, SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType)
    {
        StreamSubscriptionBuilder streamSubscriptionBuilder = new(services, subscriptionId, streamSubscriptionType);
        services.TryAddSingleton<SubscriptionManager>();
        services.AddKeyedSingleton(subscriptionId.Value, (sp, _) => streamSubscriptionBuilder.Build(sp));
        return streamSubscriptionBuilder;
    }
}
