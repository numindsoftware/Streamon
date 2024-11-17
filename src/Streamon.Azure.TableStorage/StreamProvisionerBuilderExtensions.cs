using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Azure.TableStorage;

public static class StreamProvisionerBuilderExtensions
{
    public static StreamProvisionerBuilder AddTableStorageStreamStore(this StreamProvisionerBuilder builder, TableServiceClient tableServiceClient, TableStreamStoreOptions? options = default)
    {
        builder.ProvisionerBuilder = sp =>
        {
            options ??= new(sp.GetRequiredService<IStreamTypeProvider>());
            return ActivatorUtilities.CreateInstance<TableStreamStoreProvisioner>(sp, tableServiceClient, options);
        };
        return builder;
    }

    public static StreamProvisionerBuilder AddTableStorageStreamStore(this StreamProvisionerBuilder builder, string connectionString, TableStreamStoreOptions? options = default)
        => builder.AddTableStorageStreamStore(new TableServiceClient(connectionString), options);
}
