namespace Streamon;

public record EventEnvelope(
    EventId EventId,
    StreamPosition StreamPosition,
    DateTimeOffset Timestamp,
    object Payload,
    EventMetadata? Metadata = default);