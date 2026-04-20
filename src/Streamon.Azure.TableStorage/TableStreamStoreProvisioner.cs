using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

public class TableStreamStoreProvisioner(TableServiceClient tableServiceClient, TableStreamStoreOptions options) : IStreamStoreProvisioner
{
    public async Task<TableStreamStore> GetStoreAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!await tableServiceClient.CheckTableExistsAsync(name, cancellationToken)) throw new TableStorageProvisioningException($"Table {name} does not exist");
        var tableClient = tableServiceClient.GetTableClient(name);
        return new TableStreamStore(tableClient, options);
    }

    public async Task<IStreamStore> CreateStoreAsync(string name = nameof(Streamon), CancellationToken cancellationToken = default)
    {
        await tableServiceClient.CreateTableIfNotExistsAsync(name, cancellationToken);
        var tableClient = tableServiceClient.GetTableClient(name);
        await SeedGlobalPositionEntityAsync(tableClient, cancellationToken).ConfigureAwait(false);
        return new TableStreamStore(tableClient, options);
    }

    public Task DeleteStore(string name, CancellationToken cancellationToken = default) =>
        tableServiceClient.DeleteTableAsync(name, cancellationToken);

    /// <summary>
    /// Seeds the <c>__GLOBAL__/SO-META</c> entity with <c>GlobalSequence = 0</c> if it does not already exist.
    /// </summary>
    private async Task SeedGlobalPositionEntityAsync(TableClient tableClient, CancellationToken cancellationToken)
    {
        var existing = await tableClient.GetEntityIfExistsAsync<GlobalPositionEntity>(
            options.GlobalPartitionKey, options.GlobalMetaRowKey, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!existing.HasValue)
        {
            var entity = new GlobalPositionEntity
            {
                PartitionKey = options.GlobalPartitionKey,
                RowKey = options.GlobalMetaRowKey,
                GlobalSequence = 0,
                UpdatedOn = DateTimeOffset.UtcNow
            };
            await tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
        }
    }
}
