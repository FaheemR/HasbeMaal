using HasbeMaal.Core.Domain;

namespace HasbeMaal.Infrastructure.Persistence;

public sealed class EncryptedTransactionRepository : ITransactionRepository
{
    private const string PartitionKey = "transactions:v1";

    private readonly IEncryptedStore store;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public EncryptedTransactionRepository(IEncryptedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public async Task SaveAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transactions = await LoadTransactionsAsync(cancellationToken).ConfigureAwait(false);
            var existingIndex = transactions.FindIndex(existing => existing.Id == transaction.Id);

            if (existingIndex >= 0)
            {
                transactions[existingIndex] = transaction;
            }
            else
            {
                transactions.Add(transaction);
            }

            await store.SaveAsync(PartitionKey, transactions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task SaveManyAsync(
        IReadOnlyList<FinancialTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (transactions.Count == 0)
        {
            return;
        }

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stored = await LoadTransactionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var transaction in transactions)
            {
                ArgumentNullException.ThrowIfNull(transaction);

                var existingIndex = stored.FindIndex(existing => existing.Id == transaction.Id);
                if (existingIndex >= 0)
                {
                    stored[existingIndex] = transaction;
                }
                else
                {
                    stored.Add(transaction);
                }
            }

            await store.SaveAsync(PartitionKey, stored, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<FinancialTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transactions = await LoadTransactionsAsync(cancellationToken).ConfigureAwait(false);

        return transactions.FirstOrDefault(transaction => transaction.Id == id);
    }

    public async Task<IReadOnlyList<FinancialTransaction>> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("From date must be on or before to date.", nameof(from));
        }

        var transactions = await LoadTransactionsAsync(cancellationToken).ConfigureAwait(false);

        return transactions
            .Where(transaction => IsInRange(transaction, from, to))
            .OrderByDescending(transaction => transaction.OccurredAt)
            .ThenBy(transaction => transaction.Id)
            .ToList();
    }

    private async Task<List<FinancialTransaction>> LoadTransactionsAsync(CancellationToken cancellationToken)
    {
        return await store.LoadAsync<List<FinancialTransaction>>(PartitionKey, cancellationToken)
                .ConfigureAwait(false)
            ?? [];
    }

    private static bool IsInRange(FinancialTransaction transaction, DateOnly from, DateOnly to)
    {
        var occurredDate = DateOnly.FromDateTime(transaction.OccurredAt.Date);

        return occurredDate >= from && occurredDate <= to;
    }
}