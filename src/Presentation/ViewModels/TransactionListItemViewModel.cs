using HasbeMaal.Core.Domain;

namespace HasbeMaal.Presentation.ViewModels;

public sealed record TransactionListItemViewModel(
    string Merchant,
    string Category,
    string AmountText,
    string DirectionText,
    DateOnly OccurredOn)
{
    public static TransactionListItemViewModel FromTransaction(FinancialTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var signedAmount = transaction.Direction == TransactionDirection.Credit
            ? transaction.Amount.Amount
            : -transaction.Amount.Amount;

        return new TransactionListItemViewModel(
            transaction.Merchant,
            transaction.Category,
            $"{signedAmount:0.00} {transaction.Amount.Currency}",
            transaction.Direction == TransactionDirection.Credit ? "Credit" : "Debit",
            DateOnly.FromDateTime(transaction.OccurredAt.Date));
    }
}