using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Tests;

public class SubscriptionTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    [Fact, Priority(7)]
    public async Task ProjectEventsForStream()
    {
        var subscription = containerFixture.SubscriptionManager.Get(SubscriptionId.From("test-subscription"));
        await subscription.PollAsync();

        var projection = OrderInMemoryProjector.Projections[new StreamId("order-124")];
        Assert.NotNull(projection);
    }
}
