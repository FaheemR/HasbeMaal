namespace HasbeMaal.Core.Domain;

public sealed record FinancialTransaction(
    Guid Id,
    MoneyAmount Amount,
    TransactionDirection Direction,
    TransactionSource Source,
    DateTimeOffset OccurredAt,
    string Merchant,
    string Category,
    string? SourceReferenceHash);