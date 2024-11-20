using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Testcontainers.CosmosDb;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Streamon.Azure.CosmosDb.Tests;

public class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture() => TestContainer = new CosmosDbBuilder().WithName("streamon-cosmosdb").Build();

    public async Task DisposeAsync() => await TestContainer.DisposeAsync();

    public async Task InitializeAsync()
    {
        
        await TestContainer.StartAsync();
        CosmosClient = new CosmosClient(TestContainer.GetConnectionString());
        var typeProvider = new StreamTypeProvider(new(JsonSerializerDefaults.Web));
        StreamStoreProvisioner = new CosmosDbStreamStoreProvisioner(CosmosClient, new CosmosDbStreamStoreOptions(typeProvider, "TestDatabase"));
    }

    public CosmosDbContainer TestContainer { get; private set; }

    public CosmosClient CosmosClient { get; private set; } = null!;

    public IStreamStoreProvisioner StreamStoreProvisioner { get; private set; } = null!;
}
