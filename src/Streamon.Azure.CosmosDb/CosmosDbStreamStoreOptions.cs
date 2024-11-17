namespace Streamon.Azure.CosmosDb;

public class CosmosDbStreamStoreOptions(IStreamTypeProvider streamTypeProvider)
{
    public IStreamTypeProvider StreamTypeProvider { get; set; } = streamTypeProvider;
    /// <summary>
    /// Set a delegate to be called when events are appended to a stream.
    /// </summary>
    public Action<Stream> OnEventsAppended { get; set; } = _ => { };
    /// <summary>
    /// Set a delegate to be called when a stream is deleted.
    /// </summary>
    public Action<StreamId> OnStreamDeleted { get; set; } = _ => { };
}
