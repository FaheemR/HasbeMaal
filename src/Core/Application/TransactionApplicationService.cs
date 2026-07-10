using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Application;

public sealed class TransactionApplicationService : ITransactionApplicationService
{
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private readonly ITransactionRepository transactionRepository;

    public TransactionApplicationService(ITransactionRepository transactionRepository)
    {
        ArgumentNullException.ThrowIfNull(transactionRepository);

        this.transactionRepository = transactionRepository;
    }

    public async Task<TransactionSaveResult> SaveAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await saveGate.WaitAsync(cancellationToken);

        try
        {
            var existingTransactions = await transactionRepository.ListAsync(
                DateOnly.MinValue,
                DateOnly.MaxValue,
                cancellationToken);

            if (TransactionDuplicateDetector.HasDuplicate(transaction, existingTransactions))
            {
                return TransactionSaveResult.DuplicateIgnored(transaction);
            }

            await transactionRepository.SaveAsync(transaction, cancellationToken);

            return TransactionSaveResult.Saved(transaction);
        }
        finally
        {
            saveGate.Release();
        }
    }

    public async Task<IReadOnlyList<TransactionSaveResult>> SaveManyAsync(
        IReadOnlyList<FinancialTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (transactions.Count == 0)
        {
            return [];
        }

        await saveGate.WaitAsync(cancellationToken);

        try
        {
            // Existing transactions are loaded exactly once for the whole batch; per-item
            // de-duplication then runs in memory against the growing accepted set. This keeps
            // bulk review accepts linear instead of reloading the store for every item.
            var existingTransactions = await transactionRepository.ListAsync(
                DateOnly.MinValue,
                DateOnly.MaxValue,
                cancellationToken);

            var deduplicated = new List<FinancialTransaction>(existingTransactions);
            var results = new List<TransactionSaveResult>(transactions.Count);
            var toPersist = new List<FinancialTransaction>();

            foreach (var transaction in transactions)
            {
                if (TransactionDuplicateDetector.HasDuplicate(transaction, deduplicated))
                {
                    results.Add(TransactionSaveResult.DuplicateIgnored(transaction));
                    continue;
                }

                toPersist.Add(transaction);
                deduplicated.Add(transaction);
                results.Add(TransactionSaveResult.Saved(transaction));
            }

            if (toPersist.Count > 0)
            {
                await transactionRepository.SaveManyAsync(toPersist, cancellationToken);
            }

            return results;
        }
        finally
        {
            saveGate.Release();
        }
    }

    public async Task DeleteManyAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return;
        }

        await saveGate.WaitAsync(cancellationToken);

        try
        {
            await transactionRepository.DeleteManyAsync(ids, cancellationToken);
        }
        finally
        {
            saveGate.Release();
        }
    }

    public Task<FinancialTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        transactionRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        transactionRepository.ListAsync(from, to, cancellationToken);
}