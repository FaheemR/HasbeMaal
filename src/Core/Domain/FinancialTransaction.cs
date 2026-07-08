namespace HasbeMaal.Core.Domain;

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
        string? sourceReferenceHash)
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
    }

    public Guid Id { get; }

    public MoneyAmount Amount { get; }

    public TransactionDirection Direction { get; }

    public TransactionSource Source { get; }

    public DateTimeOffset OccurredAt { get; }

    public string Merchant { get; }

    public string Category { get; }

    public string? SourceReferenceHash { get; }
}