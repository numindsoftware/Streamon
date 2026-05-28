namespace Streamon;

public interface IStreamStoreProvisioner
{
    Task<IStreamStore> CreateStoreAsync(string name = "", CancellationToken cancellationToken = default);
}
