using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public static class StreamSubscriptionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use Azure Table Storage as the checkpoint store.
    /// </summary>
    /// <param name="builder">The <see cref="StreamSubscriptionBuilder"/> to configure.</param>
    /// <param name="connectionString">The connection string for the Azure Table Storage account.</param>
    /// <param name="streamTableName">The name of the table used to store stream metadata.</param>
    /// <param name="checkpointTableName">The name of the table used to store checkpoint data. If not specified, a default table name is used.</param>
    /// <returns>The configured <see cref="StreamSubscriptionBuilder"/> instance.</returns>
    public static StreamSubscriptionBuilder UseTableStorageCheckpointStore(this StreamSubscriptionBuilder builder, string connectionString, string streamTableName, string? checkpointTableName = default)
    {
        checkpointTableName ??= TableCheckpointStore.DefaultCheckpointTableName;
        builder.UseCheckpointStore(() => new TableCheckpointStore(new TableClient(connectionString, checkpointTableName), streamTableName));
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use a Table Storage-based subscription stream reader.
    /// </summary>
    /// <param name="builder">The <see cref="StreamSubscriptionBuilder"/> to configure.</param>
    /// <param name="connectionString">The connection string for the Azure Table Storage account.</param>
    /// <param name="streamTableName">The name of the table in Azure Table Storage that contains the stream data.</param>
    /// <param name="configureOptions">An optional delegate to configure additional options for the <see cref="TableStreamStoreOptions"/>.</param>
    /// <returns>The configured <see cref="StreamSubscriptionBuilder"/> instance.</returns>
    public static StreamSubscriptionBuilder UseTableStorageSubscriptionStreamReader(this StreamSubscriptionBuilder builder, string connectionString, string streamTableName, Action<TableStreamStoreOptions>? configureOptions = default)
    {
        var options = new TableStreamStoreOptions();
        configureOptions?.Invoke(options);
        builder.UseSubscriptionStreamReader(() => new TableSubscriptionStreamReader(new TableClient(connectionString, streamTableName), options));
        return builder;
    }
}
