using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

public class TableStreamStoreProvisioner(
    string name,
    TableServiceClient tableServiceClient,
    TableStreamStoreOptions options) : IStreamStoreProvisioner
{
    public async Task<IStreamStore> CreateStoreAsync(string suffix = "", CancellationToken cancellationToken = default)
    {
        var resolvedName = options.ComposeStreamTableName(name, suffix);
        await tableServiceClient.CreateTableIfNotExistsAsync(resolvedName, cancellationToken);
        var tableClient = tableServiceClient.GetTableClient(resolvedName);
        await SeedGlobalPositionEntityAsync(tableClient, cancellationToken).ConfigureAwait(false);
        return new TableStreamStore(tableClient, options);
    }

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
