using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;

namespace Streamon.Azure.CosmosDb.Tests;

internal class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture() => TestContainer = new CosmosDbBuilder().WithName("streamon-cosmosdb").Build();

    public async Task DisposeAsync() => await TestContainer.DisposeAsync();

    public async Task InitializeAsync()
    {
        
        await TestContainer.StartAsync();
        CosmosClient = new CosmosClient(TestContainer.GetConnectionString());
    }

    public CosmosDbContainer TestContainer { get; private set; }

    public CosmosClient? CosmosClient { get; private set; }
}
