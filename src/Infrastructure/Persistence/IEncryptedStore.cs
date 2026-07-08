namespace HasbeMaal.Infrastructure.Persistence;

public interface IEncryptedStore
{
    Task SaveAsync<T>(string partitionKey, T value, CancellationToken cancellationToken = default);

    Task<T?> LoadAsync<T>(string partitionKey, CancellationToken cancellationToken = default);
}