namespace Streamon;

public record EventEnvelope(
    EventId EventId,
    StreamPosition StreamPosition,
    StreamPosition GlobalPosition,
    DateTimeOffset Timestamp,
    object Payload,
    EventMetadata? Metadata = default);