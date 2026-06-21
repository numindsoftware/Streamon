namespace Streamon.Azure.TableStorage.Subscription;

public class TableCheckpointStoreOptions
{
    /// <summary>The default name (prefix) for the subscription's checkpoint table.</summary>
    public string CheckpointTableName { get; set; } = "StreamonCheckpoint";

    /// <summary>
    /// Composes the checkpoint table name for the given suffix. When <paramref name="prefix"/> is null
    /// the <see cref="CheckpointTableName"/> is used.
    /// </summary>
    public string ComposeCheckpointTableName(string? name, string? suffix = null) => $"{CheckpointTableName}{name ?? string.Empty}{suffix ?? string.Empty}";
}
