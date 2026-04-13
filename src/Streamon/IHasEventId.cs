namespace Streamon;

/// <summary>
/// Defines a contract for types that expose an associated event identifier.
/// </summary>
/// <remarks>Implementing this interface allows a type to provide its event identifier, which can be used for
/// logging, diagnostics, or event tracking purposes.</remarks>
public interface IHasEventId
{
    public EventId EventId { get; }
}
