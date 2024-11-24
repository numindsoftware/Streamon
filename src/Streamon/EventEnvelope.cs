namespace Streamon;

public record EventEnvelope(
    StreamId StreamId,
    EventId EventId,
    StreamPosition StreamPosition,
    StreamPosition GlobalPosition,
    DateTimeOffset Timestamp,
    BatchId BatchId,
    object Payload,
    EventMetadata? Metadata = default);