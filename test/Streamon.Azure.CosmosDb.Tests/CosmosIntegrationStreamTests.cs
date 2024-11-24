using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.CosmosDb.Tests;

public class CosmosIntegrationStreamTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    [Fact]
    public async Task AppendsNewEventsToStream()
    {
        var store = await containerFixture.StreamStoreProvisioner.CreateStoreAsync(nameof(CosmosIntegrationStreamTests));
        IEnumerable<object> events = 
        [
            OrderEvents.OrderCaptured,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderShipped,
            OrderEvents.OrderFulfilled
        ];
        var stream = await store.AppendEventsAsync(new("order-123"), StreamPosition.Start, events);

        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }
}