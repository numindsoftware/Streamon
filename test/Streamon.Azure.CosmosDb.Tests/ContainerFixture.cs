using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Testcontainers.CosmosDb;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Streamon.Azure.CosmosDb.Tests;

public class ContainerFixture : IAsyncLifetime
{
    public ContainerFixture() => TestContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
        .WithName("streamon-cosmosdb")
        .Build();

    public async ValueTask DisposeAsync() => await TestContainer.DisposeAsync();

    public async ValueTask InitializeAsync()
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
