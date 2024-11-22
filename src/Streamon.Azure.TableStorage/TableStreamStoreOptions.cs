namespace Streamon.Azure.TableStorage;

public class TableStreamStoreOptions(IStreamTypeProvider streamTypeProvider)
{
    public string TableName { get; set; } = nameof(Streamon);
    public IStreamTypeProvider StreamTypeProvider { get; set; } = streamTypeProvider;
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
    /// Set a delegate to be called when events are appended to a stream.
    /// </summary>
    public Action<Stream> OnEventsAppended { get; set; } = _ => { };
    /// <summary>
    /// Set a delegate to be called when a stream is deleted.
    /// </summary>
    public Action<StreamId> OnStreamDeleted { get; set; } = _ => { };
}
