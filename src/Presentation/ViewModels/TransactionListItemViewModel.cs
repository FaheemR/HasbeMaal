using HasbeMaal.Core.Domain;

namespace HasbeMaal.Presentation.ViewModels;

public sealed record TransactionListItemViewModel(
    Guid Id,
    string Merchant,
    string Category,
    string AmountText,
    string DirectionText,
    DateOnly OccurredOn,
    string? ReferenceText,
    string? AccountText)
{
    /// <summary>True when a raw source reference (for example a UPI reference) is available to show.</summary>
    public bool HasReference => !string.IsNullOrWhiteSpace(ReferenceText);

    /// <summary>True when a masked account or card tail is available to show.</summary>
    public bool HasAccount => !string.IsNullOrWhiteSpace(AccountText);

    public static TransactionListItemViewModel FromTransaction(FinancialTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var signedAmount = transaction.Direction == TransactionDirection.Credit
            ? transaction.Amount.Amount
            : -transaction.Amount.Amount;

        return new TransactionListItemViewModel(
            transaction.Id,
            transaction.Merchant,
            transaction.Category,
            $"{signedAmount:0.00} {transaction.Amount.Currency}",
            transaction.Direction == TransactionDirection.Credit ? "Credit" : "Debit",
            DateOnly.FromDateTime(transaction.OccurredAt.Date),
            transaction.SourceReference,
            transaction.Account);
    }
}