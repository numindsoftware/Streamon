namespace Streamon;

[Serializable]
public class StreamPositionOutOfRangeException(StreamPosition streamPosition, string? message = default) : Exception(message)
{
    public StreamPosition StreamPosition { get; } = streamPosition;
}