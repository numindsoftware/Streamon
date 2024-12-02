using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class StreamSubscriptionBuilder(SubscriptionId subscriptionId, IServiceCollection services)
{
    public SubscriptionId SubscriptionId => subscriptionId;
    public IServiceCollection Services => services;

    public StreamSubscriptionBuilder AddEventHandler<T>() where T : class, IEventHandler
    {
        services.AddKeyedSingleton<T>(subscriptionId);
        return this;
    }

    //public StreamSubscriptionBuilder AddEventHandler<T>(Func<IServiceProvider, SubscriptionId, IEventHandler> implementationFactory) where T : class, IEventHandler
    //{
    //    services.AddKeyedSingleton(subscriptionId, (sp, key) => implementationFactory(sp, SubscriptionId.From(key!.ToString()!)));
    //    return this;
    //}

    //public StreamSubscriptionBuilder AddCheckpointStore<T>(Func<IServiceProvider, SubscriptionId, ICheckpointStore> implementationFactory) where T : class, ICheckpointStore
    //{
    //    services.AddKeyedSingleton(subscriptionId, (sp, key) => implementationFactory(sp, SubscriptionId.From(key!.ToString()!)));
    //    return this;
    //}

    //public StreamSubscriptionBuilder AddSubscriptionStreamReader<T>(Func<IServiceProvider, SubscriptionId, ISubscriptionStreamReader> implementationFactory) where T : class, ISubscriptionStreamReader
    //{
    //    services.AddKeyedSingleton<ISubscriptionStreamReader, T>(subscriptionId);
    //    return this;
    //}
}
