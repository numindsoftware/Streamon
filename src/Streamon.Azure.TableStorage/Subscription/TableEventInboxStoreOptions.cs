namespace Streamon.Azure.TableStorage.Subscription;

public class TableEventInboxStoreOptions
{
    /// <summary>The default name (prefix) for the subscription's inbox table.</summary>
    public string DefaultInboxName { get; set; } = "StreamonInbox";

    /// <summary>
    /// Strategy used to compose the physical inbox table name from
    /// <see cref="DefaultInboxName"/> and a provisioning-time suffix. Default: prefix + suffix.
    /// </summary>
    public Func<string, string, string> DefaultInboxNamingStrategy { get; set; } = NamingConventions.Concatenate;

    /// <summary>
    /// Composes the inbox table name for the given suffix. When <paramref name="prefix"/> is null the
    /// <see cref="DefaultInboxName"/> is used.
    /// </summary>
    public string ComposeInboxName(string? suffix, string? prefix = null) =>
        DefaultInboxNamingStrategy(prefix ?? DefaultInboxName, suffix ?? string.Empty);
}
