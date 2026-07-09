using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Application;

public sealed record TransactionSaveResult
{
    private TransactionSaveResult(
        TransactionSaveStatus status,
        FinancialTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        Status = status;
        Transaction = transaction;
    }

    public TransactionSaveStatus Status { get; }

    public FinancialTransaction Transaction { get; }

    public static TransactionSaveResult Saved(FinancialTransaction transaction) =>
        new(TransactionSaveStatus.Saved, transaction);

    public static TransactionSaveResult DuplicateIgnored(FinancialTransaction transaction) =>
        new(TransactionSaveStatus.DuplicateIgnored, transaction);
}