namespace Streamon.Azure.TableStorage;

public class TableStreamStoreOptions(IStreamTypeProvider streamTypeProvider)
{
    public IStreamTypeProvider StreamTypeProvider { get; set; } = streamTypeProvider;
    /// <summary>
    /// Retrieving the global position is an expensive operation, and it is not always needed.
    /// When this is turned on, for every update/append, the store will calculate the global position for each event by retrieving all event stream entities.
    /// </summary>
    public bool CalculateGlobalPosition { get; set; } = false;
    /// <summary>
    /// Disabling soft delete will have both performance penalties and will make it impossible to recover deleted streams.
    /// Azure table does not allow for batch delete operations, so each entity will be deleted one by one.
    /// </summary>
    public bool DisableSoftDelete { get; set; } = false;

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
