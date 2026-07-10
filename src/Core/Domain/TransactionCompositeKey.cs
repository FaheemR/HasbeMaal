namespace HasbeMaal.Core.Domain;

/// <summary>
/// A deterministic value key used to detect duplicate transactions when no source reference
/// hash is available. Two transactions with the same amount, currency, direction, minute of
/// occurrence (in UTC), and normalized merchant are treated as the same event. Implemented as
/// a <see langword="readonly record struct"/> so it provides free value equality and a stable
/// hash code for use in a <see cref="System.Collections.Generic.HashSet{T}"/>.
/// </summary>
public readonly record struct TransactionCompositeKey(
    decimal Amount,
    string Currency,
    TransactionDirection Direction,
    DateTimeOffset OccurredAtMinute,
    string NormalizedMerchant)
{
    /// <summary>
    /// Builds a composite key from a transaction. The occurrence timestamp is converted to UTC
    /// and truncated to the minute (seconds and sub-second components zeroed) at a zero offset so
    /// equal instants compare equal regardless of the originating offset. The merchant is trimmed
    /// and upper-cased using the invariant culture.
    /// </summary>
    public static TransactionCompositeKey FromTransaction(FinancialTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var utc = transaction.OccurredAt.ToUniversalTime();
        var occurredAtMinute = new DateTimeOffset(
            utc.Year,
            utc.Month,
            utc.Day,
            utc.Hour,
            utc.Minute,
            0,
            TimeSpan.Zero);

        return new TransactionCompositeKey(
            transaction.Amount.Amount,
            transaction.Amount.Currency,
            transaction.Direction,
            occurredAtMinute,
            transaction.Merchant.Trim().ToUpperInvariant());
    }
}
