using System.Text.Json;
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

public static class StreamSubscriptionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use Azure Table Storage as the checkpoint store.
    /// </summary>
    public static StreamSubscriptionBuilder UseTableStorageCheckpointStore(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        string streamTableName,                                  // prefix for the *paired* stream name
        string? checkpointTableName = default)
    {
        checkpointTableName ??= TableCheckpointStore.DefaultCheckpointTableName;
        builder.UseCheckpointStore(suffix =>
        {
            var checkpointTable = builder.ComposeName(checkpointTableName, suffix);
            var pairedStreamName = builder.ComposeName(streamTableName, suffix);
            return new TableCheckpointStore(
                new TableClient(connectionString, checkpointTable),
                pairedStreamName);
        });
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="StreamSubscriptionBuilder"/> to use a Table Storage-based subscription stream reader.
    /// </summary>
    public static StreamSubscriptionBuilder UseTableStorageSubscriptionStreamReader(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        string streamTableName,
        Action<TableStreamStoreOptions>? configureOptions = default)
    {
        var options = new TableStreamStoreOptions();
        configureOptions?.Invoke(options);
        builder.UseSubscriptionStreamReader(suffix => new TableSubscriptionStreamReader(
            new TableClient(connectionString, builder.ComposeName(streamTableName, suffix)), options));
        return builder;
    }

    public static StreamSubscriptionBuilder UseTableStorageEventInbox(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        string? inboxTableName = default)
    {
        inboxTableName ??= TableStorageEventInbox.DefaultInboxTableName;
        builder.UseEventInbox(suffix => new TableStorageEventInbox(
            new TableClient(connectionString, builder.ComposeName(inboxTableName, suffix))));
        return builder;
    }

    /// <summary>
    /// Registers a projection backed by Azure Table Storage. Partition key and row key are derived
    /// from <typeparamref name="TState"/> domain properties via the provided selectors, keeping
    /// key mapping explicit and type-safe. Properties of types not natively supported by Table Storage
    /// (complex objects, collections) are automatically serialized to JSON strings using the
    /// configured <paramref name="jsonSerializerOptions"/>.
    /// </summary>
    /// <typeparam name="TProjector">The projector type implementing
    /// <see cref="IEventInitialProjector{TEvent, TState}"/> and/or
    /// <see cref="IEventProjector{TEvent, TState}"/>.</typeparam>
    /// <typeparam name="TState">The table entity type representing the projection state.</typeparam>
    /// <param name="builder">The subscription builder to configure.</param>
    /// <param name="connectionString">The Azure Table Storage connection string.</param>
    /// <param name="tableName">The name of the table used to store projection state.</param>
    /// <param name="partitionKeySelector">Selects the partition key value from a <typeparamref name="TState"/> instance.</param>
    /// <param name="rowKeySelector">Selects the row key value from a <typeparamref name="TState"/> instance.</param>
    /// <param name="jsonSerializerOptions">Optional JSON serializer options for complex property serialization.
    /// When <c>null</c>, <see cref="System.Text.Json"/> defaults are used.</param>
    /// <returns>The configured <see cref="StreamSubscriptionBuilder"/> instance.</returns>
    public static StreamSubscriptionBuilder AddTableStorageProjection<TProjector, TState>(
        this StreamSubscriptionBuilder builder,
        string connectionString,
        string tableName,
        Func<TState, string> partitionKeySelector,
        Func<TState, string> rowKeySelector,
        JsonSerializerOptions? jsonSerializerOptions = null)
        where TProjector : class
        where TState : class, ITableEntity, new()
    {
        return builder.AddProjection<TProjector, TState>(
            () => new TableStorageProjectionStore<TState>(
                new TableClient(connectionString, tableName),
                partitionKeySelector,
                rowKeySelector,
                jsonSerializerOptions));
    }
}
