using Microsoft.Extensions.DependencyInjection;
using Streamon.Tests.Fixtures;

namespace Streamon.Azure.CosmosDb.Tests;

public class CosmosIntegrationStreamTests(ContainerFixture containerFixture) : IClassFixture<ContainerFixture>
{
    [Fact]
    public async Task AppendsNewEventsToStream()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddStreamon().AddCosmosDbStreamStore(containerFixture.CosmosClient); //"AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEca4x9Dp6Iy3sA==;");
        var provider = services.BuildServiceProvider();
        var provisioner = provider.GetRequiredService<IStreamStoreProvisioner>();

        var store = await provisioner.CreateStoreAsync(nameof(CosmosIntegrationStreamTests));
        IEnumerable<object> events = 
        [
            OrderEvents.OrderCaptured,
            OrderEvents.OrderConfirmed,
            OrderEvents.OrderShipped,
            OrderEvents.OrderFulfilled
        ];
        var stream = await store.AppendAsync(new("order-123"), StreamPosition.Start, events);

        Assert.NotEmpty(stream);
        Assert.NotEqual(stream.First().EventId, default);
    }
}