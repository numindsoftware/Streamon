using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Subscription;

public class SubscriptionManager(IServiceProvider keyedServiceProvider)
{
    public StreamSubscription Get(SubscriptionId subscriptionId) =>
        keyedServiceProvider.GetRequiredKeyedService<StreamSubscription>(subscriptionId.Value);
    
    public IEnumerable<StreamSubscription> All() =>
        keyedServiceProvider.GetServices<StreamSubscription>();

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        foreach (var subscription in All())
        {
            await subscription.PollAsync(cancellationToken);
        }
    }
}
