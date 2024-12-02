using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public static class StreamSubscriptionBuilderExtensions
{
    public static StreamSubscriptionBuilder UseTableStorageCheckpointStore(this StreamSubscriptionBuilder builder, string connectionString, string streamTableName, string? checkpointTableName = default)
    {
        checkpointTableName ??= TableCheckpointStore.DefaultCheckpointTableName;
        builder.Services.AddKeyedSingleton<ICheckpointStore>(builder.SubscriptionId.Value, (_, _) => new TableCheckpointStore(new TableClient(connectionString, checkpointTableName), streamTableName));
        return builder;
    }

    public static StreamSubscriptionBuilder UseTableStorageSubscriptionStreamReader(this StreamSubscriptionBuilder builder, string connectionString, string streamTableName, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        var optionsBuilder = builder.Services.AddOptions<TableStreamStoreOptions>();
        if (configureOptions is not null) optionsBuilder.Configure(configureOptions);
        builder.Services.AddKeyedSingleton<ISubscriptionStreamReader>(builder.SubscriptionId.Value, (sp, _) => 
        {
            var options = sp.GetRequiredService<IOptions<TableStreamStoreOptions>>().Value;
            return new TableSubscriptionStreamReader(new TableClient(connectionString, streamTableName), options);
        });
        return builder;
    }
}
