using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public static class StreamSubscriptionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use a Table Storage-based subscription stream reader.
    /// When <paramref name="streamTableName"/> is null the default from
    /// <see cref="StreamSubscriptionOptions.DefaultStreamName"/> is used.
    /// </summary>
    public static StreamSubscriptionBuilder UseTableStorageSubscriptionStreamReader(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        Action<TableStreamStoreOptions>? configureOptions = default)
    {
        var options = new TableStreamStoreOptions();
        configureOptions?.Invoke(options);
        builder.UseSubscriptionStreamReader(suffix => new TableSubscriptionStreamReader(new TableServiceClient(connectionString), options.ComposeStreamTableName(suffix), options));
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use Azure Table Storage as the checkpoint store.
    /// When <paramref name="streamTableName"/> or <paramref name="checkpointTableName"/> are null the corresponding
    /// defaults from <see cref="StreamSubscriptionOptions"/> are used.
    /// </summary>
    public static StreamSubscriptionBuilder UseTableStorageCheckpointStore(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        Action<TableCheckpointStoreOptions>? configureOptions = default)
    {
        var options = new TableCheckpointStoreOptions();
        configureOptions?.Invoke(options);
        builder.UseCheckpointStore(suffix => new TableCheckpointStore(new TableServiceClient(connectionString), options.ComposeCheckpointTableName(suffix)));
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use Azure Table Storage as the event inbox.
    /// When <paramref name="inboxTableName"/> is null the default from
    /// <see cref="StreamSubscriptionOptions.DefaultInboxName"/> is used.
    /// </summary>
    public static StreamSubscriptionBuilder UseTableStorageEventInbox(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        Action<TableEventInboxStoreOptions>? configureOptions = default)
    {
        var options = new TableEventInboxStoreOptions();
        configureOptions?.Invoke(options);
        builder.UseEventInbox(suffix => new TableEventInboxStore(new TableServiceClient(connectionString), options.ComposeInboxName(suffix)));
        return builder;
    }

    /// <summary>
    /// Registers a projection backed by Azure Table Storage. Partition key and row key are derived
    /// from <typeparamref name="TState"/> domain properties via the provided selectors, keeping
    /// key mapping explicit and type-safe. Properties of types not natively supported by Table Storage
    /// (complex objects, collections) are automatically serialized to JSON strings using the
    /// configured <paramref name="jsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>
    /// Use <paramref name="namingStrategy"/> to opt-in to per-suffix table scoping for this projection:
    /// <list type="bullet">
    /// <item><description><c>null</c> (default) — table name is used as-is regardless of the subscription suffix
    /// (scenarios 1, 2 and 3: projections share a single physical table across all scopes).</description></item>
    /// <item><description><c>(prefix, suffix) =&gt; prefix + suffix</c> — one physical table per suffix
    /// (scenario 4: <c>XYZProjectionContoso</c>, <c>XYZProjectionFabrikam</c>, …).</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The table entity type representing the projection state.</typeparam>
    /// <param name="builder">The subscription builder to configure.</param>
    /// <param name="connectionString">The Azure Table Storage connection string.</param>
    /// <param name="tableName">The base table name (prefix) used to store projection state.</param>
    /// <param name="partitionKeySelector">Selects the partition key value from a <typeparamref name="TState"/> instance.</param>
    /// <param name="rowKeySelector">Selects the row key value from a <typeparamref name="TState"/> instance.</param>
    /// <param name="jsonSerializerOptions">Optional JSON serializer options for complex property serialization.
    /// When <c>null</c>, <see cref="System.Text.Json"/> defaults are used.</param>
    /// <param name="namingStrategy">Optional projection-specific naming strategy receiving
    /// (<paramref name="tableName"/>, build-time suffix). When <c>null</c>, the table name is used unchanged.</param>
    /// <returns>The configured <see cref="StreamSubscriptionBuilder"/> instance.</returns>
    public static StreamSubscriptionBuilder AddTableStorageProjection<TProjector, TState>(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        Action<TableProjectionStoreOptions>? configureOptions = default)
        where TProjector : class
        where TState : class, ITableEntity, new()
    {
        return builder.AddProjection<TProjector, TState>(
            suffix =>
            {
                var options = new TableProjectionStoreOptions();
                configureOptions?.Invoke(options);
                var resolvedName = options.ComposeProjectionTableName<TState>(suffix);
                return new TableProjectionStore<TState>(
                    new TableClient(connectionString, resolvedName),
                    options.JsonSerializerOptions);
            });
    }
}
