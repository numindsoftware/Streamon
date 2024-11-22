using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage;

internal class TableCheckpointStoreProvisioner(TableServiceClient tableServiceClient, TableCheckpointStoreOptions options)
{
    public async Task<ICheckpointStore> CreateCheckpointStore(string name, CancellationToken cancellationToken = default)
    {
        await tableServiceClient.CreateTableIfNotExistsAsync(name, cancellationToken);
        var tableClient = tableServiceClient.GetTableClient(name);
        return new TableCheckpointStore(tableClient, options);
    }

    public async Task<ICheckpointStore> GetCheckpointStore(string name, CancellationToken cancellationToken = default)
    {
        if (!await tableServiceClient.CheckTableExistsAsync(name, cancellationToken)) throw new TableStorageProvisioningException($"Table {name} does not exist");
        var tableClient = tableServiceClient.GetTableClient(name);
        return new TableCheckpointStore(tableClient, options);
    }

    public async Task DeleteCheckpointStore(string name, CancellationToken cancellationToken = default) =>
        await tableServiceClient.DeleteTableAsync(name, cancellationToken);
}
