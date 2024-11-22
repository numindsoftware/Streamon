
using Azure.Data.Tables;
using Streamon.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Azure.TableStorage;

#pragma warning disable CS9113 // Parameter is unread.
internal class TableCheckpointStore(TableClient tableClient, TableCheckpointStoreOptions options) : ICheckpointStore
#pragma warning restore CS9113 // Parameter is unread.
{
    public Task<Checkpoint> GetCheckpointAsync(string subscriptionId)
    {
        //var response = await tableClient.QueryAsync<CheckpointEntity>(c => c.PartitionKey == subscriptionId).;
        //if (response.HasValue) throw new CheckpointNotFoundException(subscriptionId);
        //return response.Value.Checkpoint;
        throw new NotImplementedException();
    }

    public Task SetCheckpointAsync(Checkpoint checkpoint)
    {
        throw new NotImplementedException();
    }
}
