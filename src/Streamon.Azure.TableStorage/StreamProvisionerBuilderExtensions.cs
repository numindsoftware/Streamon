using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Streamon.Azure.TableStorage;

public static class StreamProvisionerBuilderExtensions
{
    public static StreamProvisionerBuilder AddTableStorageEventStore(this StreamProvisionerBuilder builder, TableServiceClient tableServiceClient, TableStreamStoreOptions? options = default)
    {
        builder.ProvisionerBuilder = sp =>
        {
            options ??= new TableStreamStoreOptions(sp.GetRequiredService<IStreamTypeProvider>());
            return ActivatorUtilities.CreateInstance<TableStreamStoreProvisioner>(sp, tableServiceClient, options);
        };
        return builder;
    }
    public static StreamProvisionerBuilder AddTableStorageEventStore(this StreamProvisionerBuilder builder, string connectionString, TableStreamStoreOptions? options = default)
        => builder.AddTableStorageEventStore(new TableServiceClient(connectionString), options).AddStreamTypeProvider();
}
