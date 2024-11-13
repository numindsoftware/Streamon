using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Azure.TableStorage;

/// <summary>
/// This class is responsible for appending events to the event store
/// It makes sure all needed entities are persisted paired correctly, in a single transaction and in the correct order.
/// When the update count is higher than the batch size, the batch is split into smaller batches.
/// </summary>
internal class AppendBatch
{


    public void AddEvents(EventEntity @event)
    {
        throw new NotImplementedException();
    }


}
