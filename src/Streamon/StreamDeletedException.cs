namespace Streamon;

public class StreamDeletedException(StreamId streamId, string? message = default) : Exception(message)
{
    public StreamId StreamId { get; } = streamId;
}
