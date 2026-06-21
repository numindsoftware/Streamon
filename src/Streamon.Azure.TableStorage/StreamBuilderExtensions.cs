using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Streamon.Azure.TableStorage;

public static class StreamBuilderExtensions
{
    public static StreamBuilder UseTableStorageStreamStore(this StreamBuilder builder, string connectionString, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        if (configureOptions is not null) builder.Services.Configure(configureOptions);
        if (string.IsNullOrWhiteSpace(builder.Name))
        {
            builder.Services.TryAddScoped<IStreamStoreProvisioner>(sp =>
            {
                TableServiceClient tableServiceClient = new(connectionString);
                var tableStreamStoreOptions = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
                return new TableStreamStoreProvisioner(tableStreamStoreOptions.StreamTableName, tableServiceClient, tableStreamStoreOptions);
            });
        }
        else
        {
            builder.Services.TryAddKeyedScoped<IStreamStoreProvisioner>(builder.Name, (sp, _) =>
            {
                TableServiceClient tableServiceClient = new(connectionString);
                var tableStreamStoreOptions = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
                return new TableStreamStoreProvisioner(builder.Name, tableServiceClient, tableStreamStoreOptions);
            });
        }
        return builder;
    }

    public static StreamBuilder UseNamedTableStorageStreamStore(this StreamBuilder builder, string connectionName, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        if (configureOptions is not null) builder.Services.Configure(configureOptions);
        if (string.IsNullOrWhiteSpace(builder.Name))
        {
            builder.Services.TryAddScoped<IStreamStoreProvisioner>(sp =>
            {
                var tableServiceClient = sp.GetRequiredKeyedService<TableServiceClient>(connectionName);
                var tableStreamStoreOptions = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
                return new TableStreamStoreProvisioner(tableStreamStoreOptions.StreamTableName, tableServiceClient, tableStreamStoreOptions);
            });
        }
        else
        {
            builder.Services.TryAddKeyedScoped<IStreamStoreProvisioner>(builder.Name, (sp, _) =>
            {
                var tableServiceClient = sp.GetRequiredKeyedService<TableServiceClient>(connectionName);
                var tableStreamStoreOptions = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
                return new TableStreamStoreProvisioner(builder.Name, tableServiceClient, tableStreamStoreOptions);
            });
        }
        return builder;
    }
}
