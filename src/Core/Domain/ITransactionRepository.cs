namespace HasbeMaal.Core.Domain;

public interface ITransactionRepository
{
    Task SaveAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default);

    Task<FinancialTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialTransaction>> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}