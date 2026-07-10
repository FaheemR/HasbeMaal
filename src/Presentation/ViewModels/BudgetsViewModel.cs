using System.Collections.ObjectModel;
using System.Windows.Input;
using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class BudgetsViewModel : ViewModelBase
{
    private readonly IMonthlyBudgetCategoryRepository budgetCategoryRepository;
    private readonly ITransactionApplicationService transactionApplicationService;
    private bool isLoading;

    public BudgetsViewModel(
        IMonthlyBudgetCategoryRepository budgetCategoryRepository,
        ITransactionApplicationService transactionApplicationService)
    {
        ArgumentNullException.ThrowIfNull(budgetCategoryRepository);
        ArgumentNullException.ThrowIfNull(transactionApplicationService);

        this.budgetCategoryRepository = budgetCategoryRepository;
        this.transactionApplicationService = transactionApplicationService;
        Items = [];
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    public ObservableCollection<BudgetProgressItemViewModel> Items { get; }

    public ICommand RefreshCommand { get; }

    public bool IsEmpty => !IsLoading && Items.Count == 0;

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
            var budgetCategories = await budgetCategoryRepository.GetAsync(
                year,
                month,
                cancellationToken);
            var transactions = await transactionApplicationService.ListAsync(
                firstDay,
                lastDay,
                cancellationToken);

            Load(budgetCategories, transactions, year, month);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public void Load(
        MonthlyBudgetCategories budgetCategories,
        IEnumerable<FinancialTransaction> transactions,
        int year,
        int month)
    {
        ArgumentNullException.ThrowIfNull(budgetCategories);
        ArgumentNullException.ThrowIfNull(transactions);

        var summary = MonthlySummaryCalculator.Calculate(transactions, year, month);
        var spendingByCategory = summary.Categories.ToDictionary(
            category => category.Category,
            category => category.Expenses,
            StringComparer.OrdinalIgnoreCase);

        Load(budgetCategories.Categories.Select(category =>
        {
            var spent = spendingByCategory.GetValueOrDefault(
                category.Name,
                new MoneyAmount(0m, category.MonthlyLimit.Currency));

            return (category, spent);
        }));
    }

    public void Load(IEnumerable<(BudgetCategory Category, MoneyAmount Spent)> budgets)
    {
        ArgumentNullException.ThrowIfNull(budgets);

        Items.Clear();

        foreach (var (category, spent) in budgets)
        {
            Items.Add(BudgetProgressItemViewModel.FromCategory(category, spent));
        }

        OnPropertyChanged(nameof(IsEmpty));
    }
}