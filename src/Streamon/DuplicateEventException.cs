namespace Streamon;

[Serializable]
public class DuplicateEventException(EventId eventId, string? message = default) : Exception(message)
{
    public EventId EventId { get; } = eventId;
}
