using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class StreamSubscriptionBuilder(SubscriptionId subscriptionId, IServiceCollection services)
{
    public StreamSubscriptionBuilder AddEventHandler<T>() where T : class, IEventHandler
    {
        services.AddKeyedSingleton<IEventHandler, T>(subscriptionId);
        return this;
    }

    public StreamSubscriptionBuilder AddCheckpointStore<T>() where T : class, ICheckpointStore
    {
        services.AddKeyedSingleton<ICheckpointStore, T>(subscriptionId);
        return this;
    }

    public StreamSubscriptionBuilder AddSubscriptionStreamReader<T>() where T : class, ISubscriptionStreamReader
    {
        services.AddKeyedSingleton<ISubscriptionStreamReader, T>(subscriptionId);
        return this;
    }
}
