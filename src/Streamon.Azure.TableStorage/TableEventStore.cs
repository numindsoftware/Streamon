using Azure.Data.Tables;
using Streamon;

namespace Streamon.Azure.TableStorage;

public class TableEventStore : IStreamStore
{
    //private readonly TableClient _table;

    public Task<Stream> FetchAsync(StreamId streamId, StreamPosition startPosition = default, StreamPosition endPosition = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> AppendAsync(StreamId id, StreamPosition expectedPosition, IEnumerable<object> events, EventMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteStreamAsync(StreamId streamId, StreamPosition expectedSequence, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
