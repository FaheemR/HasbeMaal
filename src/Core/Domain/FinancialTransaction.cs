namespace HasbeMaal.Core.Domain;

using System.Text;

public sealed record FinancialTransaction
{
    public FinancialTransaction(
        Guid id,
        MoneyAmount amount,
        TransactionDirection direction,
        TransactionSource source,
        DateTimeOffset occurredAt,
        string merchant,
        string category,
        string? sourceReferenceHash,
        string? sourceReference = null,
        string? account = null,
        string? sourceMessage = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Transaction id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(amount);

        if (string.IsNullOrWhiteSpace(merchant))
        {
            throw new ArgumentException("Merchant is required.", nameof(merchant));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        Id = id;
        Amount = amount;
        Direction = direction;
        Source = source;
        OccurredAt = occurredAt;
        Merchant = merchant.Trim();
        Category = category.Trim();
        SourceReferenceHash = string.IsNullOrWhiteSpace(sourceReferenceHash)
            ? null
            : sourceReferenceHash.Trim().ToUpperInvariant();
        SourceReference = string.IsNullOrWhiteSpace(sourceReference)
            ? null
            : sourceReference.Trim();
        Account = string.IsNullOrWhiteSpace(account)
            ? null
            : account.Trim();
        SourceMessage = string.IsNullOrWhiteSpace(sourceMessage)
            ? null
            : sourceMessage.Trim();
    }

    public Guid Id { get; }

    public MoneyAmount Amount { get; }

    public TransactionDirection Direction { get; }

    public TransactionSource Source { get; }

    public DateTimeOffset OccurredAt { get; }

    public string Merchant { get; }

    public string Category { get; }

    public string? SourceReferenceHash { get; }

    /// <summary>
    /// Raw source reference (for example a UPI reference number) retained for display to the user.
    /// Stored only inside encrypted persistence, shown only in the local UI, never logged, and
    /// removed by delete/purge. Excluded from <see cref="ToString"/> so it cannot leak via logs.
    /// </summary>
    public string? SourceReference { get; }

    /// <summary>
    /// Masked account or card tail this transaction posted to (for example a card's last four
    /// digits, "ICICI Bank Credit Card ••5005"), so the user can tell which of their accounts moved.
    /// Stored only inside encrypted persistence, shown only in the local UI, never logged, and
    /// removed by delete/purge. Excluded from <see cref="ToString"/>.
    /// </summary>
    public string? Account { get; }

    /// <summary>
    /// The original SMS body of a matched, imported transaction, retained so the user can review it
    /// on the local detail page. Stored only inside encrypted persistence, shown only in the local
    /// UI (read-only, no copy/share), never logged, never transmitted, and removed by delete/purge.
    /// Excluded from <see cref="ToString"/> so it cannot leak via logs.
    /// </summary>
    public string? SourceMessage { get; }

    /// <summary>
    /// Overrides the record's synthesized member printing so the raw <see cref="SourceReference"/>,
    /// its hash, the masked <see cref="Account"/>, and the raw <see cref="SourceMessage"/> are never
    /// emitted by <see cref="ToString"/> or a structured-log placeholder.
    /// </summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Id = ").Append(Id);
        builder.Append(", Amount = ").Append(Amount);
        builder.Append(", Direction = ").Append(Direction);
        builder.Append(", Source = ").Append(Source);
        builder.Append(", OccurredAt = ").Append(OccurredAt);
        builder.Append(", Merchant = ").Append(Merchant);
        builder.Append(", Category = ").Append(Category);
        return true;
    }
}