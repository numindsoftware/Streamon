namespace Streamon;

public class StreamEventArgs(Stream stream) : EventArgs
{
    public Stream Stream { get; } = stream;
}
