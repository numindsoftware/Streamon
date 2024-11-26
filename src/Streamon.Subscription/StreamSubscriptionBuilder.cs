using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class StreamSubscriptionBuilder
{
    public StreamSubscriptionBuilder AddEventHandler<T>() where T : IEventHandler
    {
        return this;
    }
}
