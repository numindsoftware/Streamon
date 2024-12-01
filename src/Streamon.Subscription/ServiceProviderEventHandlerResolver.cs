using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

internal class ServiceProviderEventHandlerResolver(SubscriptionId subscriptionId, IServiceProvider serviceProvider) : IEventHandlerResolver
{
    public IEventHandler Resolve(Type handlerType) => 
        (IEventHandler)serviceProvider.GetRequiredKeyedService(handlerType, subscriptionId);
}
