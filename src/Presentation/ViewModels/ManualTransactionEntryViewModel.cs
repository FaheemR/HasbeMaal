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
    private bool amountTouched;
    private bool merchantTouched;
    private bool categoryTouched;
    private bool showAllValidationErrors;
    private bool isSaving;
    private string? amountValidationError;
    private string? merchantValidationError;
    private string? categoryValidationError;
    private string? amountError;
    private string? merchantError;
    private string? categoryError;
    private string? saveStatusMessage;
    private FinancialTransaction? lastCreatedTransaction;

    public ManualTransactionEntryViewModel(ITransactionApplicationService transactionApplicationService)
    {
        ArgumentNullException.ThrowIfNull(transactionApplicationService);

        this.transactionApplicationService = transactionApplicationService;
        SaveCommand = new AsyncRelayCommand(SaveTransactionAsync, CanSave);
        Validate(revealErrors: false);
    }

    public string Amount
    {
        get => amount;
        set
        {
            if (SetProperty(ref amount, value))
            {
                amountTouched = true;
                ClearSaveStatusMessage();
                Validate(revealErrors: false);
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
                merchantTouched = true;
                ClearSaveStatusMessage();
                Validate(revealErrors: false);
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
                categoryTouched = true;
                ClearSaveStatusMessage();
                Validate(revealErrors: false);
            }
        }
    }

    public DateTime OccurredOn
    {
        get => occurredOn;
        set
        {
            if (SetProperty(ref occurredOn, value.Date))
            {
                ClearSaveStatusMessage();
            }
        }
    }

    public bool IsCredit
    {
        get => isCredit;
        set
        {
            if (SetProperty(ref isCredit, value))
            {
                ClearSaveStatusMessage();
            }
        }
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

    public bool IsInvalid => amountValidationError is not null || merchantValidationError is not null || categoryValidationError is not null;

    public bool HasAmountError => AmountError is not null;

    public bool HasMerchantError => MerchantError is not null;

    public bool HasCategoryError => CategoryError is not null;

    public bool IsSaving
    {
        get => isSaving;
        private set => SetProperty(ref isSaving, value);
    }

    public string? SaveStatusMessage
    {
        get => saveStatusMessage;
        private set
        {
            if (SetProperty(ref saveStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasSaveStatusMessage));
            }
        }
    }

    public bool HasSaveStatusMessage => SaveStatusMessage is not null;

    public FinancialTransaction? LastCreatedTransaction
    {
        get => lastCreatedTransaction;
        private set => SetProperty(ref lastCreatedTransaction, value);
    }

    public ICommand SaveCommand { get; }

    public bool Validate()
    {
        return Validate(revealErrors: true);
    }

    private bool Validate(bool revealErrors)
    {
        if (revealErrors)
        {
            showAllValidationErrors = true;
        }

        amountValidationError = ParseAmount() is null
            ? "Enter a positive amount."
            : null;
        merchantValidationError = string.IsNullOrWhiteSpace(Merchant)
            ? "Enter a merchant or note."
            : null;
        categoryValidationError = string.IsNullOrWhiteSpace(Category)
            ? "Enter a category."
            : null;

        AmountError = showAllValidationErrors || amountTouched ? amountValidationError : null;
        MerchantError = showAllValidationErrors || merchantTouched ? merchantValidationError : null;
        CategoryError = showAllValidationErrors || categoryTouched ? categoryValidationError : null;

        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(IsInvalid));
        OnPropertyChanged(nameof(HasAmountError));
        OnPropertyChanged(nameof(HasMerchantError));
        OnPropertyChanged(nameof(HasCategoryError));
        ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();

        return !IsInvalid;
    }

    private bool CanSave() => !IsInvalid;

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

        IsSaving = true;
        try
        {
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

            if (saveResult.Status == TransactionSaveStatus.Saved)
            {
                LastCreatedTransaction = saveResult.Transaction;
                ResetForm();
                SaveStatusMessage = "Entry saved.";
                return;
            }

            LastCreatedTransaction = null;
            SaveStatusMessage = "Entry was already saved.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetForm()
    {
        SetProperty(ref amount, string.Empty, nameof(Amount));
        SetProperty(ref merchant, string.Empty, nameof(Merchant));
        SetProperty(ref category, DefaultCategory, nameof(Category));
        SetProperty(ref occurredOn, DateTime.Today, nameof(OccurredOn));
        SetProperty(ref isCredit, false, nameof(IsCredit));
        amountTouched = false;
        merchantTouched = false;
        categoryTouched = false;
        showAllValidationErrors = false;
        Validate(revealErrors: false);
    }

    private void ClearSaveStatusMessage()
    {
        SaveStatusMessage = null;
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