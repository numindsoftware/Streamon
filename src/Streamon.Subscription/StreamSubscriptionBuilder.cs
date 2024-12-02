using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class StreamSubscriptionBuilder(IServiceCollection services, SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType)
{
    public IServiceCollection Services { get; } = services;
    public SubscriptionId SubscriptionId { get; } = subscriptionId;
    public StreamSubscriptionType StreamSubscriptionType { get; } = streamSubscriptionType;
    public List<Type> StreamEventHandlers { get; } = [];

    public StreamSubscriptionBuilder AddEventHandler<T>() where T : class, IEventHandler
    {
        StreamEventHandlers.Add(typeof(T));
        return this;
    }

    public StreamSubscription Build(IServiceProvider serviceProvider)
    {
        var checkpointStore = serviceProvider.GetRequiredKeyedService<ICheckpointStore>(SubscriptionId.Value);
        var streamEventHandlerResolver = serviceProvider.GetRequiredKeyedService<IEventHandlerResolver>(SubscriptionId.Value);
        var subscriptionStreamReader = serviceProvider.GetRequiredKeyedService<ISubscriptionStreamReader>(SubscriptionId.Value);
        StreamSubscription subscription = new(SubscriptionId, StreamSubscriptionType, streamEventHandlerResolver, checkpointStore, subscriptionStreamReader);
        StreamEventHandlers.ForEach(t => subscription.AddEventHandler(t));
        return subscription;
    }
}
