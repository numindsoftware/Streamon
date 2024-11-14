namespace Streamon.Azure.TableStorage;

public class TableStreamStoreOptions
{
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
}
