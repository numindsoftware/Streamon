using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Azure.CosmosDb;

#pragma warning disable CS9113 // Parameter is unread.
internal class CosmosDbTableStore(CosmosClient tableClient, CosmosDbStreamStoreOptions options) : IStreamStore
#pragma warning restore CS9113 // Parameter is unread.
{
    public event EventHandler<StreamEventArgs>? EventsAppended;
    public event EventHandler<StreamIdEventArgs>? StreamDeleted;

    public Task<Stream> AppendAsync(StreamId id, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> DeleteStreamAsync(StreamId streamId, StreamPosition expectedPosition, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> FetchAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected virtual void OnEventsAppended(Stream stream)
    {
        options.OnEventsAppended?.Invoke(stream);
        EventsAppended?.Invoke(this, new(stream));
    }
    protected virtual void OnStreamDeleted(StreamId streamId)
    {
        options.OnStreamDeleted?.Invoke(streamId);
        StreamDeleted?.Invoke(this, new(streamId));
    }
}
