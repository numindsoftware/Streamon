namespace Streamon.Azure.TableStorage.Subscription;

public class TableCheckpointStoreOptions
{
    /// <summary>The default name (prefix) for the subscription's checkpoint table.</summary>
    public string CheckpointTableName { get; set; } = "StreamonCheckpoint";

    /// <summary>
    /// Strategy used to compose the physical checkpoint table name from
    /// <see cref="CheckpointTableName"/> and a provisioning-time suffix. Default: prefix + suffix.
    /// </summary>
    public Func<string, string, string> CheckpointTableNamingStrategy { get; set; } = NamingConventions.Concatenate;

    /// <summary>
    /// Composes the checkpoint table name for the given suffix. When <paramref name="prefix"/> is null
    /// the <see cref="CheckpointTableName"/> is used.
    /// </summary>
    public string ComposeCheckpointTableName(string? suffix, string? prefix = null) =>
        CheckpointTableNamingStrategy(prefix ?? CheckpointTableName, suffix ?? string.Empty);
}
