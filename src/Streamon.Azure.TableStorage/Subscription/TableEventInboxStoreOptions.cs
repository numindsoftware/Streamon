namespace Streamon.Azure.TableStorage.Subscription;

public class TableEventInboxStoreOptions
{
    /// <summary>The default name (prefix) for the subscription's inbox table.</summary>
    public string DefaultInboxName { get; set; } = "StreamonInbox";

    /// <summary>
    /// Composes the inbox table name for the given suffix. When <paramref name="prefix"/> is null the
    /// <see cref="DefaultInboxName"/> is used.
    /// </summary>
    public string ComposeInboxName(string? name, string? suffix = null) =>
        $"{DefaultInboxName}{name ?? string.Empty}{suffix ?? string.Empty}";
}
