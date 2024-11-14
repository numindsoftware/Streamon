using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

public class StreamStorageProvisioner(TableServiceClient tableServiceClient)
{
    public const string DefaultTableName = "Streams";

    public async Task<bool> CheckTableExistsAsync(string name, CancellationToken cancellationToken = default) =>
        await tableServiceClient.QueryAsync(name, 1, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken).MoveNextAsync();

    public async Task<TableStreamStore> CreateStoreAsync(string name = DefaultTableName, CancellationToken cancellationToken = default)
    {
        if (await CheckTableExistsAsync(name, cancellationToken)) throw new TableStorageProvisioningException($"Table {name} already exists");
        await tableServiceClient.CreateTableIfNotExistsAsync(name, cancellationToken);
        var tableClient = tableServiceClient.GetTableClient(name);
        return new TableStreamStore(tableClient);
    }

    public async Task<TableStreamStore> GetStoreAsync(string name = DefaultTableName, CancellationToken cancellationToken = default)
    {
        if (!await CheckTableExistsAsync(name, cancellationToken)) throw new TableStorageProvisioningException($"Table {name} does not exist");
        var tableClient = tableServiceClient.GetTableClient(name);
        return new TableStreamStore(tableClient);
    }

    public Task DeleteStorage(string name, CancellationToken cancellationToken = default) =>
        tableServiceClient.DeleteTableAsync(name, cancellationToken);
}
