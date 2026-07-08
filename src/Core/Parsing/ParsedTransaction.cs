using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Parsing;

public sealed record ParsedTransaction(
    MoneyAmount Amount,
    string Merchant,
    DateTimeOffset? OccurredAt,
    TransactionDirection Direction,
    TransactionSource Source,
    string? Reference,
    ParseConfidence Confidence);