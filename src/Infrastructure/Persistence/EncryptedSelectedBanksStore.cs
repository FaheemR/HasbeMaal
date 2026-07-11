using HasbeMaal.Core.Import;

namespace HasbeMaal.Infrastructure.Persistence;

/// <summary>
/// Stores the user's selected SMS source bank ids in the encrypted local store, in its own
/// partition, mirroring <see cref="EncryptedSmsImportWatermarkStore"/>. Only stable registry ids
/// are persisted so the choice survives across sessions; no message content ever reaches this store.
/// </summary>
public sealed class EncryptedSelectedBanksStore : ISelectedBanksStore
{
    private const string PartitionKey = "sms-selected-banks:v1";

    private readonly IEncryptedStore store;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public EncryptedSelectedBanksStore(IEncryptedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public async Task<IReadOnlyList<string>> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await store.LoadAsync<StoredSelectedBanks>(PartitionKey, cancellationToken)
            .ConfigureAwait(false);

        return stored?.BankIds ?? Array.Empty<string>();
    }

    public async Task SetAsync(IReadOnlyList<string> bankIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bankIds);

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await store.SaveAsync(PartitionKey, new StoredSelectedBanks(bankIds.ToArray()), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private sealed record StoredSelectedBanks(IReadOnlyList<string> BankIds);
}
