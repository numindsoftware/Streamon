﻿using Azure.Data.Tables;

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
        return new TableStreamStore(tableClient, options);
    }

    public Task DeleteStore(string name, CancellationToken cancellationToken = default) =>
        tableServiceClient.DeleteTableAsync(name, cancellationToken);
}
