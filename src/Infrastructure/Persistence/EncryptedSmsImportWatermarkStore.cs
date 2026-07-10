using HasbeMaal.Core.Import;

namespace HasbeMaal.Infrastructure.Persistence;

/// <summary>
/// Stores the SMS import watermark in the encrypted local store. Only a timestamp is persisted so
/// incremental scans survive across sessions; no message content ever reaches this store.
/// </summary>
public sealed class EncryptedSmsImportWatermarkStore : ISmsImportWatermarkStore
{
    private const string PartitionKey = "sms-import-watermark:v1";

    private readonly IEncryptedStore store;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public EncryptedSmsImportWatermarkStore(IEncryptedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public async Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await store.LoadAsync<StoredSmsImportWatermark>(PartitionKey, cancellationToken)
            .ConfigureAwait(false);

        return stored?.Watermark;
    }

    public async Task SetAsync(DateTimeOffset? watermark, CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await store.SaveAsync(PartitionKey, new StoredSmsImportWatermark(watermark), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private sealed record StoredSmsImportWatermark(DateTimeOffset? Watermark);
}
