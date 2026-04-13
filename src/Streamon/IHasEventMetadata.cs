namespace Streamon;

/// <summary>
/// Provides access to event metadata associated with the implementing type.
/// </summary>
/// <remarks>Implementing this interface allows retrieval of event-related metadata, which can be useful for event
/// handling and processing. The metadata may include details tenant information and user identity.</remarks>
public interface IHasEventMetadata
{
    public EventMetadata Metadata { get; }
}
