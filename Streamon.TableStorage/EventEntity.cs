using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.TableStorage;

internal class EventEntity : ITableEntity
{
    /// <summary>
    /// Stream Identifier
    /// </summary>
    public required string PartitionKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    /// <summary>
    /// Stram Logical Unit, can be (prefix):
    /// SO-HEAD: One per stream, controls optimistic concurrency
    /// SS-EVENT-{version}: Event line
    /// SS-UID-{uuid}: Event Identity, one per each SS-EVENT, prevents duplicate event insertion
    /// </summary>
    public required string RowKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// Fully qualified type name
    /// </summary>
    public required string Type { get; set; }
    public Guid Id { get; set; }
    public string? Metadata { get; set; }
    public string? Data { get; set; }

}
