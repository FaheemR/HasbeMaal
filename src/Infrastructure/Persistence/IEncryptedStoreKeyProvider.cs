namespace HasbeMaal.Infrastructure.Persistence;

public interface IEncryptedStoreKeyProvider
{
    Task<byte[]> GetOrCreateKeyAsync(CancellationToken cancellationToken = default);
}