using Azure;
using Azure.Data.Tables;

namespace Streamon.Azure.TableStorage;

internal class EventEntity : ITableEntity
{
    /// <summary>
    /// Stream Identifier
    /// </summary>
    public required string PartitionKey { get; set; }
    /// <summary>
    /// SS-EVENT-{sequence}: Event line
    /// </summary>
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// Gets or sets the sequence number associated with the entity.
    /// </summary>
    /// <remarks>The sequence number must be a non-negative value. It is typically used to maintain the order of events.</remarks>
    public required long Sequence { get; set; }
    public required long GlobalSequence { get; set; }
    /// <summary>
    /// An id assigned to a group of events that are appended together
    /// </summary>
    public required string BatchId { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    /// <remarks>The value is automatically set to the current date and time when the entity is
    /// instantiated.</remarks>
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
    /// <summary>
    /// Fully qualified type name
    /// </summary>
    public required string Type { get; set; }
    /// <summary>
    /// The unique identifier of the event
    /// </summary>
    public required string EventId { get; set; }
    /// <summary>
    /// The serialized event data. The content is determined by the serializer used and may include both the event payload and additional metadata, depending on the serialization format and configuration.
    /// </summary>
    public required string Data { get; set; }
    /// <summary>
    /// Gets or sets the metadata associated with the object. This property can be used to store additional information
    /// relevant to the object's state or behavior.
    /// </summary>
    /// <remarks>The value may be null if no metadata is provided. Supplying meaningful metadata can help
    /// clarify the context or usage of the object in various scenarios.</remarks>
    public string? Metadata { get; set; }
}
