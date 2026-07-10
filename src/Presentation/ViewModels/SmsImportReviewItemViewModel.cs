using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Import;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class SmsImportReviewItemViewModel : ViewModelBase
{
    private bool isSelected;

    private SmsImportReviewItemViewModel(SmsImportReviewCandidate candidate)
    {
        Candidate = candidate;

        var transaction = candidate.Transaction;
        var signedAmount = transaction.Direction == TransactionDirection.Credit
            ? transaction.Amount.Amount
            : -transaction.Amount.Amount;

        Merchant = transaction.Merchant;
        Category = transaction.Category;
        AmountText = $"{signedAmount:0.00} {transaction.Amount.Currency}";
        DirectionText = transaction.Direction == TransactionDirection.Credit ? "Credit" : "Debit";
        OccurredOn = DateOnly.FromDateTime(transaction.OccurredAt.Date);
        ConfidenceText = candidate.Confidence.ToString();
    }

    public SmsImportReviewCandidate Candidate { get; }

    public FinancialTransaction Transaction => Candidate.Transaction;

    public ParseConfidence Confidence => Candidate.Confidence;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public string Merchant { get; }

    public string Category { get; }

    public string AmountText { get; }

    public string DirectionText { get; }

    public DateOnly OccurredOn { get; }

    public string ConfidenceText { get; }

    public static SmsImportReviewItemViewModel FromCandidate(SmsImportReviewCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return new SmsImportReviewItemViewModel(candidate);
    }
}
