using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public static class ServiceCollectionExtensions
{
    public static StreamSubscriptionBuilder AddStreamSubscription(this IServiceCollection services, SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType)
    {
        services.AddKeyedSingleton<StreamSubscription>(subscriptionId, (sp, _) =>
        {
            var eventHandlerResolver = sp.GetKeyedService<IEventHandlerResolver>(subscriptionId) ?? ActivatorUtilities.CreateInstance<ServiceProviderEventHandlerResolver>(sp);
            var checkpointStore = sp.GetRequiredKeyedService<ICheckpointStore>(subscriptionId);
            var subscriptionStreamReader = sp.GetRequiredKeyedService<ISubscriptionStreamReader>(subscriptionId);
            return new(subscriptionId, streamSubscriptionType, eventHandlerResolver, checkpointStore, subscriptionStreamReader);
        });
        return new(subscriptionId, services);
    }
}
