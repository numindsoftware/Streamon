using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class StreamSubscriptionBuilder
{
    public StreamSubscriptionBuilder(IServiceCollection services, SubscriptionId subscriptionId, StreamSubscriptionType streamSubscriptionType) =>
        (Services, SubscriptionId, StreamSubscriptionType) = (services, subscriptionId, streamSubscriptionType);

    public IServiceCollection Services { get; }
    public SubscriptionId SubscriptionId { get; }
    public StreamSubscriptionType StreamSubscriptionType { get; }
    public List<Type> StreamEventHandlers { get; } = [];

    public StreamSubscriptionBuilder AddEventHandler<T>() where T : class
    {
        StreamEventHandlers.Add(typeof(T));
        return this;
    }

    public StreamSubscription Build(IServiceProvider serviceProvider)
    {
        var checkpointStore = serviceProvider.GetRequiredKeyedService<ICheckpointStore>(SubscriptionId.Value);
        var subscriptionStreamReader = serviceProvider.GetRequiredKeyedService<ISubscriptionStreamReader>(SubscriptionId.Value);
        var eventHandlerRegistry = serviceProvider.GetRequiredKeyedService<IEventHandlerRegistry>(SubscriptionId.Value);
        StreamSubscription subscription = new(SubscriptionId, StreamSubscriptionType, checkpointStore, subscriptionStreamReader, eventHandlerRegistry, serviceProvider);
        //StreamEventHandlers.ForEach(t => subscription.AddEventHandler(t));
        return subscription;
    }
}
