using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Streamon.Azure.TableStorage;

public static class StreamProvisionerBuilderExtensions
{
    public static StreamProvisionerBuilder AddTableStorageStreamStore(this StreamProvisionerBuilder builder, TableServiceClient tableServiceClient, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        var optionsBuilder = builder.Services.AddOptions<TableStreamStoreOptions>();
        if (configureOptions is not null) optionsBuilder.Configure(configureOptions);
        builder.ProvisionerBuilder = sp =>
        {
            var options = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
            return ActivatorUtilities.CreateInstance<TableStreamStoreProvisioner>(sp, tableServiceClient, options);
        };
        return builder;
    }

    public static StreamProvisionerBuilder AddTableStorageStreamStore(this StreamProvisionerBuilder builder, string connectionString, Action<TableStreamStoreOptions>? optionsBuilder = default)
        => builder.AddTableStorageStreamStore(new TableServiceClient(connectionString), optionsBuilder);
}
