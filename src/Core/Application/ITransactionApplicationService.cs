using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Application;

public interface ITransactionApplicationService
{
    Task<TransactionSaveResult> SaveAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<FinancialTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialTransaction>> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}