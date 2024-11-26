using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

internal class TableCheckpointStoreProvisioner(TableServiceClient tableServiceClient, TableCheckpointStoreOptions options)
{
    public async Task<ICheckpointStore> CreateCheckpointStore(string streamTableName, CancellationToken cancellationToken = default)
    {
        await tableServiceClient.CreateTableIfNotExistsAsync(options.TableName, cancellationToken);
        var tableClient = tableServiceClient.GetTableClient(streamTableName);
        return new TableCheckpointStore(streamTableName, tableClient);
    }

    //public async Task<ICheckpointStore> GetCheckpointStore(string streamTableName, CancellationToken cancellationToken = default)
    //{
    //    if (!await tableServiceClient.CheckTableExistsAsync(options.TableName, cancellationToken)) throw new TableStorageProvisioningException($"Table {options.TableName} does not exist");
    //    var tableClient = tableServiceClient.GetTableClient(streamTableName);
    //    return new TableCheckpointStore(streamTableName, tableClient);
    //}

    public async Task DeleteCheckpointStore(string name, CancellationToken cancellationToken = default) =>
        await tableServiceClient.DeleteTableAsync(name, cancellationToken);
}
