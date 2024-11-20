using Azure;
using System.Text.Json.Serialization;

namespace Streamon.Azure.CosmosDb;

internal record StreamDocument(
    [property: JsonPropertyName("id")]
    StreamId StreamId,
    /// <summary>
    /// The last event sequence number, updated on each event append
    /// </summary>
    StreamPosition Position,
    /// <summary>
    /// The global sequence number, updated on each event append for all streams.
    /// In order to calculate the global sequence number, just before saving we query the very last stream and add the new event count.
    /// </summary>
    StreamPosition GlobalPosition,
    DateTimeOffset CreatedOn,
    DateTimeOffset UpdatedOn,
    bool IsDeleted = false,
    DateTimeOffset? DeletedOn = default,
    [property: JsonPropertyName("_ts")]
    DateTimeOffset? Timestamp = default,
    [property: JsonPropertyName("_etag")]
    ETag? ETag = default);
