namespace Streamon.Azure.TableStorage.Subscription;

public class TableSubscriptionStreamReaderOptions
{
    /// <summary>The default name (prefix) for the subscription's stream table.</summary>
    public string StreamTableName { get; set; } = "Streamon";

    /// <summary>
    /// Strategy used to compose the physical stream table name from
    /// <see cref="StreamTableName"/> and a provisioning-time suffix. Default: prefix + suffix
    /// (prefix unchanged when the suffix is empty).
    /// </summary>
    public Func<string, string, string> StreamTableNamingStrategy { get; set; } = NamingConventions.Concatenate;

    /// <summary>
    /// Composes the stream table name for the given suffix. When <paramref name="prefix"/> is null the
    /// <see cref="StreamTableName"/> is used.
    /// </summary>
    public string ComposeStreamTableName(string? suffix, string? prefix = null) =>
        StreamTableNamingStrategy(prefix ?? StreamTableName, suffix ?? string.Empty);
}
