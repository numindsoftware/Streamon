namespace Streamon.Azure.TableStorage;

/// <summary>
/// Thrown when the global position allocation fails after exhausting all retries due to contention.
/// </summary>
public class GlobalPositionAllocationException(int maxRetries)
    : Exception($"Failed to allocate a global position after {maxRetries} retries due to contention.")
{
    public int MaxRetries { get; } = maxRetries;
}