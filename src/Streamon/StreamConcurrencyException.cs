namespace Streamon;

public class StreamConcurrencyException(StreamPosition expectedSequence, StreamPosition actualSequence, string? message = default, Exception? innerException = default)
    : Exception(message ?? $"Expected sequence {expectedSequence} but found {actualSequence}", innerException)
{
    public StreamPosition ExpectedSequence { get; } = expectedSequence;
    public StreamPosition ActualSequence { get; } = actualSequence;
}
