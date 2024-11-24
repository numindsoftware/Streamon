namespace Streamon;

public class StreamEventArgs(IEnumerable<Event> events) : EventArgs
{
    public IEnumerable<Event> Events { get; } = events;
}
