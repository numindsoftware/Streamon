namespace Streamon;

/// <summary>
/// Predefined naming conventions used to compose a physical resource name from a
/// registration-time <c>prefix</c> and a provisioning-time <c>suffix</c>.
/// </summary>
/// <remarks>
/// All Streamon naming strategies have the shape <c>Func&lt;string, string, string&gt;</c> where the
/// first argument is the prefix and the second is the suffix. These helpers cover the three
/// common scenarios:
/// <list type="bullet">
/// <item><description><see cref="KeepPrefix"/> — ignore the suffix and use the prefix as-is.</description></item>
/// <item><description><see cref="ReplaceWithSuffix"/> — ignore the prefix and use the suffix as-is (falls back to the prefix when the suffix is empty).</description></item>
/// <item><description><see cref="Concatenate"/> — concatenate <c>prefix + suffix</c> (returns the prefix unchanged when the suffix is empty).</description></item>
/// </list>
/// </remarks>
public static class NamingConventions
{
    /// <summary>Keeps the prefix unchanged regardless of the suffix.</summary>
    public static readonly Func<string, string, string> KeepPrefix =
        static (prefix, _) => prefix;

    /// <summary>Uses the suffix as the full name, falling back to the prefix when the suffix is empty.</summary>
    public static readonly Func<string, string, string> ReplaceWithSuffix =
        static (prefix, suffix) => string.IsNullOrEmpty(suffix) ? prefix : suffix;

    /// <summary>Concatenates prefix + suffix; returns the prefix unchanged when the suffix is empty.</summary>
    public static readonly Func<string, string, string> Concatenate =
        static (prefix, suffix) => string.IsNullOrEmpty(suffix) ? prefix : prefix + suffix;
}