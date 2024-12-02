using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public static class StreamSubscriptionBuilderExtensions
{
    public static StreamSubscriptionBuilder AddTableStorageCheckpointStore(this StreamSubscriptionBuilder builder, string connectionString, string? tableName = default)
    {
        builder.Services.AddKeyedSingleton<ICheckpointStore>(builder.SubscriptionId, (_, _) => new TableCheckpointStore(new TableClient(connectionString, tableName), tableName));
        return builder;
    }

    public static StreamSubscriptionBuilder AddTableStorageSubscriptionStreamReader(this StreamSubscriptionBuilder builder, string connectionString, string tableName, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        var optionsBuilder = builder.Services.AddOptions<TableStreamStoreOptions>();
        if (configureOptions is not null) optionsBuilder.Configure(configureOptions);
        builder.Services.AddKeyedSingleton<ISubscriptionStreamReader>(builder.SubscriptionId, (sp, _) => 
        {
            var options = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
            return ActivatorUtilities.CreateInstance<TableSubscriptionStreamReader>(sp, new TableClient(connectionString, tableName));
        });
        return builder;
    }
}
