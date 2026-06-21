namespace Streamon.Azure.TableStorage;

public class TableStreamStoreOptions
{
    /// <summary>
    /// The default name used when provisioning the stream store. Acts as the prefix for any
    /// suffix-based naming strategy.
    /// </summary>
    public string StreamTableName { get; set; } = nameof(Streamon);
    /// <summary>Composes the physical stream store name for the given suffix using the configured strategy.</summary>
    
    public string ComposeStreamTableName(string? name, string? suffix) => $"{StreamTableName}{name ?? string.Empty}{suffix ?? string.Empty}";

    public IStreamTypeProvider StreamTypeProvider { get; set; } = new StreamTypeProvider();

    /// <summary>
    /// Disabling soft delete will have both performance penalties and will make it impossible to recover deleted streams.
    /// Defaults to Soft delete mode, an IsDeleted flag will be set to true and a DeletedOn timestamp will be recorded.
    /// Internally Soft deletes will only be tracked at the Stream level, and not at the Event level.
    /// </summary>
    /// <remarks>
    /// Note that hard deletes in Table Storage is an expensive and slow operation since Azure table does not allow for batch delete operations, so each entity will be deleted one by one.
    /// </remarks>
    public StreamDeleteMode DeleteMode { get; set; } = StreamDeleteMode.Soft;

    /// <summary>
    /// Azure table storage has a maximum batch size of 100. This is the default value, but can be adjusted to a lower value if needed.
    /// Effectively events occupy two rows in the table, one for the event id and one for the event details, this effectively means that the batch size will by of 48 events or less (space is needed for other entities such as the Stream and posible snapshots).
    /// </summary>
    public byte TransactionBatchSize { get; set; } = 100;

    /// <summary>
    /// Thre key to use in the RowKey colums for stream header entities. Defaults to "SO-STREAM".
    /// </summary>
    public string StreamEntityRowKey { get; set; } = "SO-STREAM";
    /// <summary>
    /// Thre prefix to use in the RowKey colums for event id  entities. Defaults to "SO-ID-".
    /// </summary>
    public string EventIdEntityRowKeyPrefix { get; set; } = "SO-ID-";
    /// <summary>
    /// Thre prefix to use in the RowKey colums for event details entities. Defaults to "SO-EVENT-".
    /// </summary>
    public string EventEntityRowKeyPrefix { get; set; } = "SO-EVENT-";
    /// <summary>
    /// Thre prefix to use in the RowKey colums for snapshot entities. Defaults to "SO-SNAP-".
    /// </summary>
    public string SnapshotEntityPrefix { get; set; } = "SO-SNAP-";

    /// <summary>
    /// The PartitionKey for the singleton global position entity. Defaults to "__GLOBAL__".
    /// </summary>
    public string GlobalPartitionKey { get; set; } = "__GLOBAL__";
    /// <summary>
    /// The RowKey for the singleton global position entity. Defaults to "SO-META".
    /// </summary>
    public string GlobalMetaRowKey { get; set; } = "SO-META";
    /// <summary>
    /// Maximum number of ETag-based retries (with jitter) when allocating a global position range. Defaults to 10.
    /// </summary>
    public int MaxGlobalPositionRetries { get; set; } = 10;
    /// <summary>
    /// The PartitionKey used for the Global Event Index (__GLOBAL-EVENT-INDEX__) partition. Defaults to "__GLOBAL-EVENT-INDEX__".
    /// </summary>
    public string GlobalEventIndexPartitionKey { get; set; } = "__GLOBAL-EVENT-INDEX__";

    /// <summary>
    /// Set a delegate to be called when events are appended to a stream.
    /// </summary>
    public Action<IEnumerable<Event>> OnEventsAppended { get; set; } = _ => { };
    /// <summary>
    /// Set a delegate to be called when a stream is deleted.
    /// </summary>
    public Action<StreamId> OnStreamDeleted { get; set; } = _ => { };
}
