using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

internal class ServiceProviderEventHandlerResolver(IServiceProvider serviceProvider) : IEventHandlerResolver
{
    public IEventHandler Resolve(Type handlerType) => (IEventHandler)ActivatorUtilities.CreateInstance(serviceProvider, handlerType);
}
