namespace HasbeMaal.Core.Domain;

public sealed record RecurringTransactionPattern(
    string Merchant,
    string Category,
    TransactionDirection Direction,
    MoneyAmount Amount,
    DateOnly FirstOccurrence,
    DateOnly LastOccurrence,
    int OccurrenceCount,
    int ExpectedDayOfMonth);