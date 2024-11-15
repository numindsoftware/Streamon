namespace Streamon;

public class StreamIdEventArgs(StreamId streamId) : EventArgs
{
    public StreamId StreamId { get; } = streamId;
}