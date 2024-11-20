namespace Streamon.Azure.CosmosDb;

public class CosmosDbStreamStoreOptions(IStreamTypeProvider streamTypeProvider, string databaseName)
{
    /// <summary>
    /// Disabling soft delete will have both performance penalties and will make it impossible to recover deleted streams.
    /// Defaults to Soft delete mode, an IsDeleted flag will be set to true and a DeletedOn timestamp will be recorded.
    /// Internally Soft deletes will only be tracked at the Stream level, and not at the Event level.
    /// </summary>
    /// <remarks>
    /// Note that hard deletes in Table Storage is an expensive and slow operation since Azure table does not allow for batch delete operations, so each entity will be deleted one by one.
    /// </remarks>
    public StreamDeleteMode DeleteMode { get; set; } = StreamDeleteMode.Soft;
    public string DatabaseName { get; set; } = databaseName;
    public int Throughput { get; set; } = 400;
    public IStreamTypeProvider StreamTypeProvider { get; set; } = streamTypeProvider;
    /// <summary>
    /// Set a delegate to be called when events are appended to a stream.
    /// </summary>
    public Action<Stream> OnEventsAppended { get; set; } = _ => { };
    /// <summary>
    /// Set a delegate to be called when a stream is deleted.
    /// </summary>
    public Action<StreamId> OnStreamDeleted { get; set; } = _ => { };
    /// <summary>
    /// Register a projection generator for a stream type. The generator is called for every stream update and saved along its type in every stream document.
    /// </summary>
    public Func<StreamId, IEnumerable<EventEnvelope>, object>? ProjectionGenerator { get; set; }
}
