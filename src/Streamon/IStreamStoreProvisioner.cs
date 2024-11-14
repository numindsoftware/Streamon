namespace Streamon;

public interface IStreamStoreProvisioner
{
    Task<IStreamStore> CreateStoreAsync(string name = nameof(Streamon), CancellationToken cancellationToken = default);
    Task DeleteStore(string name, CancellationToken cancellationToken = default);
}
