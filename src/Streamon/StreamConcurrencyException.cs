namespace Streamon;

public class StreamConcurrencyException(StreamPosition expectedPosition, StreamPosition actualPosition, string? message = default, Exception? innerException = default)
    : Exception(message ?? $"Expected sequence {expectedPosition} but found {actualPosition}", innerException)
{
    public StreamPosition ExpectedPosition { get; } = expectedPosition;
    public StreamPosition ActualPosition { get; } = actualPosition;
}
