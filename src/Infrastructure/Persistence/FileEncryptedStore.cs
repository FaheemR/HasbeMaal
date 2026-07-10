using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HasbeMaal.Infrastructure.Persistence;

public sealed class FileEncryptedStore : IEncryptedStore
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly string rootDirectory;
    private readonly IEncryptedStoreKeyProvider? keyProvider;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly SemaphoreSlim keyInitializationLock = new(1, 1);
    private byte[]? encryptionKey;
    private byte[]? fileNameKey;

    public FileEncryptedStore(
        string rootDirectory,
        ReadOnlySpan<byte> key,
        JsonSerializerOptions? serializerOptions = null)
    {
        var validatedRootDirectory = ValidateRootDirectory(rootDirectory);

        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException("Encrypted store key must be 32 bytes.", nameof(key));
        }

        this.rootDirectory = validatedRootDirectory;
        encryptionKey = DeriveKey(key, "HasbeMaal encrypted store content key");
        fileNameKey = DeriveKey(key, "HasbeMaal encrypted store file name key");
        this.serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public FileEncryptedStore(
        string rootDirectory,
        IEncryptedStoreKeyProvider keyProvider,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);

        this.rootDirectory = ValidateRootDirectory(rootDirectory);
        this.keyProvider = keyProvider;
        this.serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task SaveAsync<T>(
        string partitionKey,
        T value,
        CancellationToken cancellationToken = default)
    {
        ValidatePartitionKey(partitionKey);
        ArgumentNullException.ThrowIfNull(value);

        Directory.CreateDirectory(rootDirectory);

        var keys = await GetDerivedKeysAsync(cancellationToken).ConfigureAwait(false);
        var normalizedPartitionKey = NormalizePartitionKey(partitionKey);
        var associatedData = Encoding.UTF8.GetBytes($"1:{normalizedPartitionKey}");
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(value, serializerOptions);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(keys.EncryptionKey, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        Array.Clear(plaintext);

        var payload = new EncryptedPayload(
            1,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext));

            await WritePayloadAsync(GetPath(normalizedPartitionKey, keys.FileNameKey), payload, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> LoadAsync<T>(
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        ValidatePartitionKey(partitionKey);

        var keys = await GetDerivedKeysAsync(cancellationToken).ConfigureAwait(false);
        var normalizedPartitionKey = NormalizePartitionKey(partitionKey);
        var path = GetPath(normalizedPartitionKey, keys.FileNameKey);
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        var payload = await JsonSerializer.DeserializeAsync<EncryptedPayload>(
                stream,
                serializerOptions,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("Encrypted store payload is missing.");

        if (payload.Version != 1)
        {
            throw new InvalidDataException("Encrypted store payload version is unsupported.");
        }

        var nonce = Convert.FromBase64String(payload.Nonce);
        var tag = Convert.FromBase64String(payload.Tag);
        var ciphertext = Convert.FromBase64String(payload.Ciphertext);
        var plaintext = new byte[ciphertext.Length];
        var associatedData = Encoding.UTF8.GetBytes($"{payload.Version}:{normalizedPartitionKey}");

        try
        {
            using var aes = new AesGcm(keys.EncryptionKey, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

            return JsonSerializer.Deserialize<T>(plaintext, serializerOptions);
        }
        finally
        {
            Array.Clear(plaintext);
        }
    }

    private string GetPath(string normalizedPartitionKey, byte[] currentFileNameKey)
    {
        var partitionKeyBytes = Encoding.UTF8.GetBytes(normalizedPartitionKey);
        var partitionHash = Convert.ToHexString(HMACSHA256.HashData(currentFileNameKey, partitionKeyBytes));

        return Path.Combine(rootDirectory, $"{partitionHash}.json");
    }

    private async ValueTask<DerivedKeys> GetDerivedKeysAsync(CancellationToken cancellationToken)
    {
        if (encryptionKey is { } currentEncryptionKey && fileNameKey is { } currentFileNameKey)
        {
            return new DerivedKeys(currentEncryptionKey, currentFileNameKey);
        }

        if (keyProvider is null)
        {
            throw new InvalidOperationException("Encrypted store keys have not been initialized.");
        }

        await keyInitializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (encryptionKey is { } initializedEncryptionKey && fileNameKey is { } initializedFileNameKey)
            {
                return new DerivedKeys(initializedEncryptionKey, initializedFileNameKey);
            }

            byte[]? rootKey = await keyProvider.GetOrCreateKeyAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (rootKey is null || rootKey.Length != KeySizeBytes)
                {
                    throw new InvalidDataException("Encrypted store key provider returned an invalid key.");
                }

                var derivedEncryptionKey = DeriveKey(rootKey, "HasbeMaal encrypted store content key");
                var derivedFileNameKey = DeriveKey(rootKey, "HasbeMaal encrypted store file name key");

                encryptionKey = derivedEncryptionKey;
                fileNameKey = derivedFileNameKey;

                return new DerivedKeys(derivedEncryptionKey, derivedFileNameKey);
            }
            finally
            {
                if (rootKey is not null)
                {
                    Array.Clear(rootKey);
                }
            }
        }
        finally
        {
            keyInitializationLock.Release();
        }
    }

    private async Task WritePayloadAsync(
        string path,
        EncryptedPayload payload,
        CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(rootDirectory, $".{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, payload, serializerOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static byte[] DeriveKey(ReadOnlySpan<byte> key, string purpose)
    {
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(purpose));
    }

    private static string ValidateRootDirectory(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Storage directory is required.", nameof(rootDirectory));
        }

        return rootDirectory;
    }

    private static string NormalizePartitionKey(string partitionKey)
    {
        return partitionKey.Trim();
    }

    private static void ValidatePartitionKey(string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            throw new ArgumentException("Partition key is required.", nameof(partitionKey));
        }
    }

    private readonly record struct DerivedKeys(byte[] EncryptionKey, byte[] FileNameKey);

    private sealed record EncryptedPayload(
        int Version,
        string Nonce,
        string Tag,
        string Ciphertext);
}