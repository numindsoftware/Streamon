namespace Streamon.Subscription;

public enum StreamSubscriptionType
{
    /// <summary>
    /// Synchronizes the current state with the latest available data.
    /// </summary>
    /// <remarks>This method ensures that the current state is updated to reflect the most recent changes.  It
    /// is typically used in scenarios where periodic updates are required to maintain consistency.</remarks>
    CatchUp,
    /// <summary>
    /// Starts from the end of the stream, only new events will be processed.
    /// </summary>
    Live,
    /// <summary>
    /// Represents an in-memory storage mechanism for temporary data.
    /// </summary>
    /// <remarks>This class provides a way to store and retrieve data in memory, typically for scenarios where
    /// persistence is not required. It is useful for caching, testing, or other temporary storage needs. The data
    /// stored in memory will be lost when the application terminates.</remarks>
    InMemory
}
