namespace Streamon;

[Serializable]
public class StreamNotFoundException(StreamId streamId, string? message = default, Exception? innerException = default) : 
    Exception(message ?? $"Stream {streamId} was not found", innerException);