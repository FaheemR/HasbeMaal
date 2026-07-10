using System.Security.Cryptography;

namespace HasbeMaal.Infrastructure.Persistence;

public sealed class PlatformEncryptedStoreKeyProvider : IEncryptedStoreKeyProvider
{
    public const string DefaultKeyName = "io.github.faheemr.hasbemaal.encrypted-store-root-key.v1";

    private const int KeySizeBytes = 32;

    private readonly IProtectedKeyValueStore protectedKeyValueStore;
    private readonly string keyName;
    private readonly SemaphoreSlim keyCreationLock = new(1, 1);

    public PlatformEncryptedStoreKeyProvider(
        IProtectedKeyValueStore protectedKeyValueStore,
        string keyName = DefaultKeyName)
    {
        ArgumentNullException.ThrowIfNull(protectedKeyValueStore);

        if (string.IsNullOrWhiteSpace(keyName))
        {
            throw new ArgumentException("Key name is required.", nameof(keyName));
        }

        this.protectedKeyValueStore = protectedKeyValueStore;
        this.keyName = keyName;
    }

    public async Task<byte[]> GetOrCreateKeyAsync(CancellationToken cancellationToken = default)
    {
        await keyCreationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var persistedKey = await protectedKeyValueStore.GetAsync(keyName, cancellationToken)
                .ConfigureAwait(false);
            if (persistedKey is not null)
            {
                return DecodePersistedKey(persistedKey);
            }

            var generatedKey = RandomNumberGenerator.GetBytes(KeySizeBytes);
            try
            {
                await protectedKeyValueStore.SetAsync(
                        keyName,
                        Convert.ToBase64String(generatedKey),
                        cancellationToken)
                    .ConfigureAwait(false);

                return generatedKey.ToArray();
            }
            finally
            {
                Array.Clear(generatedKey);
            }
        }
        finally
        {
            keyCreationLock.Release();
        }
    }

    private static byte[] DecodePersistedKey(string persistedKey)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(persistedKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Protected encrypted store key is invalid.", ex);
        }

        if (key.Length == KeySizeBytes)
        {
            return key;
        }

        Array.Clear(key);
        throw new InvalidDataException("Protected encrypted store key is invalid.");
    }
}