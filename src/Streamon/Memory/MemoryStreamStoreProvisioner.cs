using Microsoft.Extensions.Options;

namespace Streamon.Memory;

public class MemoryStreamStoreProvisioner : IStreamStoreProvisioner
{
    private readonly Dictionary<string, IStreamStore> _stores = [];

    public Task<IStreamStore> CreateStoreAsync(string suffix = "", CancellationToken cancellationToken = default) =>
        Task.FromResult(_stores.TryGetValue(suffix, out IStreamStore? store) ? store : _stores[suffix] = new MemoryStreamStore());
}
