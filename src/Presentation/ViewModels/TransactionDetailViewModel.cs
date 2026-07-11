using System.Globalization;
using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Presentation.ViewModels;

/// <summary>
/// Presents the full details of a single transaction, including the raw source reference (for
/// example a UPI reference number). The reference is shown only here in the local UI; it is never
/// logged, exported, or sent to any service.
/// </summary>
public sealed class TransactionDetailViewModel : ViewModelBase
{
    private readonly ITransactionApplicationService transactionApplicationService;

    private bool isLoading;
    private bool isFound;
    private string merchantText = string.Empty;
    private string categoryText = string.Empty;
    private string amountText = string.Empty;
    private string directionText = string.Empty;
    private string occurredOnText = string.Empty;
    private string? referenceText;
    private string? accountText;
    private string? originalSmsText;

    public TransactionDetailViewModel(ITransactionApplicationService transactionApplicationService)
    {
        ArgumentNullException.ThrowIfNull(transactionApplicationService);

        this.transactionApplicationService = transactionApplicationService;
    }

    public bool IsLoading
    {
        get => isLoading;
        private set
        {
            if (SetProperty(ref isLoading, value))
            {
                OnPropertyChanged(nameof(NotFound));
            }
        }
    }

    public bool IsFound
    {
        get => isFound;
        private set
        {
            if (SetProperty(ref isFound, value))
            {
                OnPropertyChanged(nameof(NotFound));
            }
        }
    }

    /// <summary>True when loading finished but the transaction could not be found.</summary>
    public bool NotFound => !IsLoading && !IsFound;

    public string MerchantText
    {
        get => merchantText;
        private set => SetProperty(ref merchantText, value);
    }

    public string CategoryText
    {
        get => categoryText;
        private set => SetProperty(ref categoryText, value);
    }

    public string AmountText
    {
        get => amountText;
        private set => SetProperty(ref amountText, value);
    }

    public string DirectionText
    {
        get => directionText;
        private set => SetProperty(ref directionText, value);
    }

    public string OccurredOnText
    {
        get => occurredOnText;
        private set => SetProperty(ref occurredOnText, value);
    }

    /// <summary>The raw source reference (for example a UPI reference), or null when absent.</summary>
    public string? ReferenceText
    {
        get => referenceText;
        private set
        {
            if (SetProperty(ref referenceText, value))
            {
                OnPropertyChanged(nameof(HasReference));
            }
        }
    }

    public bool HasReference => !string.IsNullOrWhiteSpace(ReferenceText);

    /// <summary>Masked account or card tail this transaction posted to, or null when absent.</summary>
    public string? AccountText
    {
        get => accountText;
        private set
        {
            if (SetProperty(ref accountText, value))
            {
                OnPropertyChanged(nameof(HasAccount));
            }
        }
    }

    public bool HasAccount => !string.IsNullOrWhiteSpace(AccountText);

    /// <summary>
    /// The original SMS body this transaction was imported from, shown read-only. Null for manual
    /// entries and transactions imported before original-message retention.
    /// </summary>
    public string? OriginalSmsText
    {
        get => originalSmsText;
        private set
        {
            if (SetProperty(ref originalSmsText, value))
            {
                OnPropertyChanged(nameof(HasOriginalSms));
            }
        }
    }

    public bool HasOriginalSms => !string.IsNullOrWhiteSpace(OriginalSmsText);

    public async Task LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            var transaction = await transactionApplicationService
                .GetByIdAsync(id, cancellationToken);

            if (transaction is null)
            {
                IsFound = false;
                return;
            }

            var signedAmount = transaction.Direction == TransactionDirection.Credit
                ? transaction.Amount.Amount
                : -transaction.Amount.Amount;

            MerchantText = transaction.Merchant;
            CategoryText = transaction.Category;
            AmountText = $"{signedAmount:0.00} {transaction.Amount.Currency}";
            DirectionText = transaction.Direction == TransactionDirection.Credit ? "Credit" : "Debit";
            OccurredOnText = transaction.OccurredAt.ToString("dd MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
            ReferenceText = transaction.SourceReference;
            AccountText = transaction.Account;
            OriginalSmsText = transaction.SourceMessage;
            IsFound = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
