namespace HasbeMaal.Core.Domain;

public static class TransactionDuplicateDetector
{
    public static bool HasDuplicate(
        FinancialTransaction candidate,
        IEnumerable<FinancialTransaction> existingTransactions)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(existingTransactions);

        if (candidate.SourceReferenceHash is null)
        {
            return false;
        }

        return existingTransactions.Any(existing =>
            existing.Id != candidate.Id &&
            string.Equals(
                existing.SourceReferenceHash,
                candidate.SourceReferenceHash,
                StringComparison.OrdinalIgnoreCase));
    }
}