namespace Streamon;

[Serializable]
public class BatchSizeExceededException(long actualBatchSize, long maxBatchSize, string? message = default) : Exception(message)
{
    public long MaxBatchSize { get; } = maxBatchSize;
    public long ActualBatchSize { get; } = actualBatchSize;
}
