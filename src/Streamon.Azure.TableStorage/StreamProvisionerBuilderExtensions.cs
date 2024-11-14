using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace Streamon.Azure.TableStorage;

public static class StreamProvisionerBuilderExtensions
{
    public static StreamProvisionerBuilder AddTableStorageEventStore(this StreamProvisionerBuilder builder, TableServiceClient tableServiceClient, TableStreamStoreOptions? options = default)
    {
        if (options is not null)
        {
            builder.ProvisionerBuilder = sp => ActivatorUtilities.CreateInstance<TableStreamStoreProvisioner>(sp, tableServiceClient, options);
        }
        else
        {
            builder.ProvisionerBuilder = sp => ActivatorUtilities.CreateInstance<TableStreamStoreProvisioner>(sp, tableServiceClient);
        }
        return builder;
    }
    public static StreamProvisionerBuilder AddTableStorageEventStore(this StreamProvisionerBuilder builder, string connectionString, TableStreamStoreOptions? options = default)
        => builder.AddTableStorageEventStore(new TableServiceClient(connectionString), options);
}
