using Azure;
using System.Text.Json.Serialization;

namespace Streamon.Azure.CosmosDb;

/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="StreamId"></param>
/// <param name="EventId"></param>
/// <param name="Position">The last event sequence number, updated on each event append</param>
/// <param name="GlobalPosition">The global sequence number, updated on each event append for all streams. In order to calculate the global sequence number, just before saving we query the very last stream and add the new event count.</param>
/// <param name="BatchId"></param>
/// <param name="Timestamp"></param>
/// <param name="ETag"></param>
/// <param name="CreatedOn"></param>
/// <param name="Type"></param>
/// <param name="Payload"></param>
/// <param name="Metadata"></param>
internal record EventDocument(
    [property: JsonPropertyName("id")]
    string Id,
    EventId EventId,
    StreamPosition Position,
    StreamPosition GlobalPosition,
    BatchId BatchId,
    DateTimeOffset CreatedOn,
    string Type,
    object Payload,
    EventMetadata? Metadata = default,
    [property: JsonPropertyName("_ts")]
    DateTimeOffset? Timestamp = default,
    [property: JsonPropertyName("_etag")]
    ETag? ETag = default);
