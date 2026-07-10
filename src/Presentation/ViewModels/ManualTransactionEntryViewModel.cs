using System.Globalization;
using System.Windows.Input;
using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class ManualTransactionEntryViewModel : ViewModelBase
{
    private const string DefaultCategory = "Uncategorized";

    private readonly ITransactionApplicationService transactionApplicationService;

    private string amount = string.Empty;
    private string merchant = string.Empty;
    private string category = DefaultCategory;
    private DateTime occurredOn = DateTime.Today;
    private bool isCredit;
    private string? amountError;
    private string? merchantError;
    private string? categoryError;
    private FinancialTransaction? lastCreatedTransaction;

    public ManualTransactionEntryViewModel(ITransactionApplicationService transactionApplicationService)
    {
        ArgumentNullException.ThrowIfNull(transactionApplicationService);

        this.transactionApplicationService = transactionApplicationService;
        SaveCommand = new AsyncRelayCommand(SaveTransactionAsync, CanSave);
        Validate();
    }

    public string Amount
    {
        get => amount;
        set
        {
            if (SetProperty(ref amount, value))
            {
                Validate();
            }
        }
    }

    public string Merchant
    {
        get => merchant;
        set
        {
            if (SetProperty(ref merchant, value))
            {
                Validate();
            }
        }
    }

    public string Category
    {
        get => category;
        set
        {
            if (SetProperty(ref category, value))
            {
                Validate();
            }
        }
    }

    public DateTime OccurredOn
    {
        get => occurredOn;
        set => SetProperty(ref occurredOn, value.Date);
    }

    public bool IsCredit
    {
        get => isCredit;
        set => SetProperty(ref isCredit, value);
    }

    public string? AmountError
    {
        get => amountError;
        private set => SetProperty(ref amountError, value);
    }

    public string? MerchantError
    {
        get => merchantError;
        private set => SetProperty(ref merchantError, value);
    }

    public string? CategoryError
    {
        get => categoryError;
        private set => SetProperty(ref categoryError, value);
    }

    public bool HasErrors => AmountError is not null || MerchantError is not null || CategoryError is not null;

    public FinancialTransaction? LastCreatedTransaction
    {
        get => lastCreatedTransaction;
        private set => SetProperty(ref lastCreatedTransaction, value);
    }

    public ICommand SaveCommand { get; }

    public bool Validate()
    {
        AmountError = ParseAmount() is null
            ? "Enter a positive amount."
            : null;
        MerchantError = string.IsNullOrWhiteSpace(Merchant)
            ? "Enter a merchant or note."
            : null;
        CategoryError = string.IsNullOrWhiteSpace(Category)
            ? "Enter a category."
            : null;

        OnPropertyChanged(nameof(HasErrors));
        ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();

        return !HasErrors;
    }

    private bool CanSave() => !HasErrors;

    private async Task SaveTransactionAsync()
    {
        if (!Validate())
        {
            return;
        }

        var parsedAmount = ParseAmount();
        if (parsedAmount is null)
        {
            return;
        }

        var transaction = new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(parsedAmount.Value),
            IsCredit ? TransactionDirection.Credit : TransactionDirection.Debit,
            TransactionSource.ManualCash,
            new DateTimeOffset(OccurredOn.Date, TimeSpan.Zero),
            Merchant,
            Category,
            sourceReferenceHash: null);

            var saveResult = await transactionApplicationService.SaveAsync(transaction);
            LastCreatedTransaction = saveResult.Transaction;
    }

    private decimal? ParseAmount()
    {
        if (!decimal.TryParse(
            Amount,
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var parsed))
        {
            return null;
        }

        return parsed > 0m ? parsed : null;
    }
}