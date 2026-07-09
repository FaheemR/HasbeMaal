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