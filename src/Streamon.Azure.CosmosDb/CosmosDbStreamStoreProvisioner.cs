using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Azure.CosmosDb;

internal class CosmosDbStreamStoreProvisioner : IStreamStoreProvisioner
{
    public Task<IStreamStore> CreateStoreAsync(string name = "Streamon", CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteStore(string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
