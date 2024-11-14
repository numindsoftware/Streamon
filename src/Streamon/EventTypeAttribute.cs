namespace Streamon;

/// <summary>
/// Use this attribute to specify a name to identify the event type when serializing/deserializing.
/// </summary>
/// <param name="name">The event name identifier, make sure this is unique to the event</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EventTypeAttribute(string name): Attribute
{
    /// <summary>
    /// The event name identifier, make sure this is unique to the event
    /// </summary>
    public string Name { get; } = name;
}
