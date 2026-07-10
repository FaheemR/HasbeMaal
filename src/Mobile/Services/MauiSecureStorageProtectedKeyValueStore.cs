using HasbeMaal.Infrastructure.Persistence;
using Microsoft.Maui.Storage;

namespace HasbeMaal.Mobile.Services;

public sealed class MauiSecureStorageProtectedKeyValueStore : IProtectedKeyValueStore
{
    private readonly ISecureStorage secureStorage;

    public MauiSecureStorageProtectedKeyValueStore(ISecureStorage secureStorage)
    {
        ArgumentNullException.ThrowIfNull(secureStorage);

        this.secureStorage = secureStorage;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        cancellationToken.ThrowIfCancellationRequested();

        var value = await secureStorage.GetAsync(key).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return value;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        await secureStorage.SetAsync(key, value).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Protected storage key is required.", nameof(key));
        }
    }
}