using System.Reflection;
using System.Text.Json;
using Azure.Data.Tables;
using Streamon.Subscription;

namespace Streamon.Azure.TableStorage.Subscription;

/// <summary>
/// An <see cref="IProjectionStore{TState}"/> backed by Azure Table Storage. Partition key and row key
/// are derived from <typeparamref name="TState"/> domain properties via the configured selectors.
/// Properties of types not natively supported by Table Storage (complex objects, collections) are
/// automatically serialized to JSON strings using the configured <see cref="JsonSerializerOptions"/>.
/// </summary>
/// <typeparam name="TState">The table entity type representing the projection state.</typeparam>
public class TableStorageProjectionStore<TState>(
    TableClient tableClient,
    Func<TState, string> partitionKeySelector,
    Func<TState, string> rowKeySelector,
    JsonSerializerOptions? jsonSerializerOptions = null) : IProjectionStore<TState>
    where TState : class, ITableEntity, new()
{
    private static readonly HashSet<string> _tableEntityPropertyNames =
        [nameof(ITableEntity.PartitionKey), nameof(ITableEntity.RowKey), nameof(ITableEntity.Timestamp), nameof(ITableEntity.ETag)];

    private static readonly PropertyInfo[] _stateProperties = [.. typeof(TState)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && !_tableEntityPropertyNames.Contains(p.Name))];

    private static readonly HashSet<Type> _nativeTypes =
    [
        typeof(string),
        typeof(bool), typeof(bool?),
        typeof(int), typeof(int?),
        typeof(long), typeof(long?),
        typeof(double), typeof(double?),
        typeof(float), typeof(float?),
        typeof(DateTime), typeof(DateTime?),
        typeof(DateTimeOffset), typeof(DateTimeOffset?),
        typeof(Guid), typeof(Guid?),
        typeof(byte[]), typeof(BinaryData)
    ];

    /// <inheritdoc/>
    public async Task<TState?> ReadAsync(TState keyState, CancellationToken cancellationToken = default)
    {
        var pk = partitionKeySelector(keyState);
        var rk = rowKeySelector(keyState);
        var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(pk, rk, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.HasValue ? DeserializeEntity(response.Value!) : default;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(TState state, CancellationToken cancellationToken = default)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var entity = SerializeEntity(state);
        await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    private TableEntity SerializeEntity(TState state)
    {
        var entity = new TableEntity(partitionKeySelector(state), rowKeySelector(state));
        foreach (var prop in _stateProperties)
        {
            var value = prop.GetValue(state);
            if (value is null) continue;

            entity[prop.Name] = IsNativeType(prop.PropertyType)
                ? value
                : JsonSerializer.Serialize(value, prop.PropertyType, jsonSerializerOptions);
        }
        return entity;
    }

    private TState DeserializeEntity(TableEntity entity)
    {
        var state = new TState
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag
        };

        foreach (var prop in _stateProperties)
        {
            if (!entity.TryGetValue(prop.Name, out var value) || value is null) continue;

            if (IsNativeType(prop.PropertyType))
            {
                prop.SetValue(state, ConvertNativeValue(value, prop.PropertyType));
            }
            else if (value is string json)
            {
                prop.SetValue(state, JsonSerializer.Deserialize(json, prop.PropertyType, jsonSerializerOptions));
            }
        }

        return state;
    }

    private static bool IsNativeType(Type type) =>
        _nativeTypes.Contains(type);

    private static object? ConvertNativeValue(object value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value.GetType() == underlying) return value;
        if (underlying == typeof(int)) return Convert.ToInt32(value);
        if (underlying == typeof(long)) return Convert.ToInt64(value);
        if (underlying == typeof(double)) return Convert.ToDouble(value);
        if (underlying == typeof(float)) return Convert.ToSingle(value);
        if (underlying == typeof(bool)) return Convert.ToBoolean(value);
        if (underlying == typeof(string)) return value.ToString();
        if (underlying == typeof(DateTime) && value is DateTimeOffset dto) return dto.DateTime;

        return value;
    }
}