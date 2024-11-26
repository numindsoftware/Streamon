using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

internal class ServiceProviderEventHandlerResolver(IServiceProvider serviceProvider) : IEventHandlerResolver
{
    public IEventHandler Resolve(Type handlerType) => 
        ActivatorUtilities.CreateInstance(serviceProvider, handlerType) as IEventHandler ?? throw new EventHandlerRegistrationException(handlerType);
}
