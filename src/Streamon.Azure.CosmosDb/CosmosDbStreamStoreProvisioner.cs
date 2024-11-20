using Microsoft.Azure.Cosmos;

namespace Streamon.Azure.CosmosDb;

public class CosmosDbStreamStoreProvisioner(CosmosClient cosmosClient, CosmosDbStreamStoreOptions options) : IStreamStoreProvisioner
{
    public async Task<IStreamStore> CreateStoreAsync(string name = nameof(Streamon), CancellationToken cancellationToken = default)
    {
        var database = cosmosClient.GetDatabase(options.DatabaseName);
        var container = await database.CreateContainerIfNotExistsAsync(new ContainerProperties(name, "/streamId"), options.Throughput);
        return new CosmosDbStreamStore(container, options);
    }

    public Task DeleteStore(string name, CancellationToken cancellationToken = default)
    {
        var database = cosmosClient.GetDatabase(options.DatabaseName);
        var container = database.GetContainer(name);
        return container.DeleteContainerAsync(cancellationToken: cancellationToken);
    }
}
