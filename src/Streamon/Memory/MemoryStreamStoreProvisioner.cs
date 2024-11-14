namespace Streamon.Memory;

public class MemoryStreamStoreProvisioner : IStreamStoreProvisioner
{
    private readonly Dictionary<string, IStreamStore> _stores = [];
    public Task<IStreamStore> CreateStoreAsync(string name = nameof(Streamon), CancellationToken cancellationToken = default) =>
        Task.FromResult(_stores.TryGetValue(name, out IStreamStore? store) ? store : _stores[name] = new MemoryStreamStore());

    public Task DeleteStore(string name, CancellationToken cancellationToken = default)
    {
        _stores.Remove(name);
        return Task.CompletedTask;
    }
}
