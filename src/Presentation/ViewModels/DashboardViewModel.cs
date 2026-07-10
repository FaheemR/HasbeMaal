using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly ITransactionApplicationService transactionApplicationService;
    private bool isLoading;
    private int transactionCount;
    private string monthTitle = "This month";
    private string incomeText = "0.00 INR";
    private string expensesText = "0.00 INR";
    private string balanceLabel = "Surplus";
    private string balanceText = "0.00 INR";

    public DashboardViewModel(ITransactionApplicationService transactionApplicationService)
    {
        ArgumentNullException.ThrowIfNull(transactionApplicationService);

        this.transactionApplicationService = transactionApplicationService;
        Categories = [];
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    public ObservableCollection<DashboardCategorySummaryViewModel> Categories { get; }

    public ICommand RefreshCommand { get; }

    public string MonthTitle
    {
        get => monthTitle;
        private set => SetProperty(ref monthTitle, value);
    }

    public string IncomeText
    {
        get => incomeText;
        private set => SetProperty(ref incomeText, value);
    }

    public string ExpensesText
    {
        get => expensesText;
        private set => SetProperty(ref expensesText, value);
    }

    public string BalanceLabel
    {
        get => balanceLabel;
        private set => SetProperty(ref balanceLabel, value);
    }

    public string BalanceText
    {
        get => balanceText;
        private set => SetProperty(ref balanceText, value);
    }

    public int TransactionCount
    {
        get => transactionCount;
        private set
        {
            if (SetProperty(ref transactionCount, value))
            {
                OnPropertyChanged(nameof(ActivityText));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public string ActivityText => TransactionCount switch
    {
        0 => "No activity yet",
        1 => "1 transaction this month",
        _ => string.Create(CultureInfo.InvariantCulture, $"{TransactionCount} transactions this month")
    };

    public bool HasCategories => Categories.Count > 0;

    public bool IsEmpty => !IsLoading && TransactionCount == 0;

    public bool IsLoading
    {
        get => isLoading;
        private set
        {
            if (SetProperty(ref isLoading, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        await LoadMonthAsync(today.Year, today.Month, cancellationToken);
    }

    public async Task LoadMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        IsLoading = true;

        try
        {
            var transactions = await transactionApplicationService.ListAsync(
                firstDay,
                lastDay,
                cancellationToken);

            Load(transactions, year, month);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public void Load(
        IEnumerable<FinancialTransaction> transactions,
        int year,
        int month)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var transactionList = transactions.ToArray();
        var summary = MonthlySummaryCalculator.Calculate(transactionList, year, month);
        var monthlyTransactionCount = transactionList.Count(transaction =>
            transaction.OccurredAt.Year == year && transaction.OccurredAt.Month == month);

        MonthTitle = FormatMonth(year, month);
        IncomeText = FormatMoney(summary.Income);
        ExpensesText = FormatMoney(summary.Expenses);

        var balance = summary.Overspend.Amount > 0m
            ? summary.Overspend
            : summary.Surplus;
        BalanceLabel = summary.Overspend.Amount > 0m ? "Overspend" : "Surplus";
        BalanceText = FormatMoney(balance);
        TransactionCount = monthlyTransactionCount;

        Categories.Clear();
        foreach (var category in summary.Categories.Select(DashboardCategorySummaryViewModel.FromSummary))
        {
            Categories.Add(category);
        }

        OnPropertyChanged(nameof(HasCategories));
    }

    private static string FormatMonth(int year, int month)
    {
        var firstDay = new DateOnly(year, month, 1);

        return string.Create(CultureInfo.InvariantCulture, $"{firstDay:yyyy MMMM}");
    }

    private static string FormatMoney(MoneyAmount amount)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{amount.Amount:0.00} {amount.Currency}");
    }
}