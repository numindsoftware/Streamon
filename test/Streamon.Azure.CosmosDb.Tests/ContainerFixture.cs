using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;

namespace Streamon.Azure.CosmosDb.Tests;

internal class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture()
    {
        CosmosDbConfiguration configuration = new() { };
        TestContainer = new CosmosDbContainer(configuration);
        CosmosClient = new CosmosClient(TestContainer.GetConnectionString());
    }

    public async Task DisposeAsync() => await TestContainer.DisposeAsync();

    public async Task InitializeAsync() => await TestContainer.StartAsync();

    public CosmosDbContainer TestContainer { get; private set; }

    public CosmosClient CosmosClient { get; private set; }
}
