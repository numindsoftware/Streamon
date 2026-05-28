using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Streamon.Azure.TableStorage;

public static class StreamBuilderExtensions
{
    public static StreamBuilder UseTableStorageStreamStore(this StreamBuilder builder, string connectionString, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        TableStreamStoreOptions tableStreamStoreOptions = new();
        configureOptions?.Invoke(tableStreamStoreOptions);
        builder.Services.TryAddScoped<IStreamStoreProvisioner>(sp => 
        {
            TableServiceClient tableServiceClient = new(connectionString);
            return new TableStreamStoreProvisioner(tableServiceClient, tableStreamStoreOptions);
        });
        return builder;
    }
}
