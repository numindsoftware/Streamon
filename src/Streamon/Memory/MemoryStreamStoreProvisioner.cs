using Microsoft.Extensions.Options;

namespace Streamon.Memory;

public class MemoryStreamStoreProvisioner(string streamName = "") : IStreamStoreProvisioner
{
    private readonly Dictionary<string, IStreamStore> _stores = [];

    public Task<IStreamStore> CreateStoreAsync(string name = "", CancellationToken cancellationToken = default) =>
        Task.FromResult(_stores.TryGetValue(streamName, out IStreamStore? store) ? store : _stores[streamName] = new MemoryStreamStore());
}
