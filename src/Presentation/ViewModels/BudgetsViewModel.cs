using System.Collections.ObjectModel;
using System.Globalization;
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
    private int currentYear = DateTime.Today.Year;
    private int currentMonth = DateTime.Today.Month;
    private string newCategoryName = string.Empty;
    private string newCategoryLimit = string.Empty;
    private bool newCategoryIsEssential;
    private string? statusText;

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
        AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync, CanAddCategory);
    }

    public ObservableCollection<BudgetProgressItemViewModel> Items { get; }

    public ICommand RefreshCommand { get; }

    public ICommand AddCategoryCommand { get; }

    public string NewCategoryName
    {
        get => newCategoryName;
        set
        {
            if (SetProperty(ref newCategoryName, value))
            {
                AfterCategoryInputChanged();
            }
        }
    }

    public string NewCategoryLimit
    {
        get => newCategoryLimit;
        set
        {
            if (SetProperty(ref newCategoryLimit, value))
            {
                AfterCategoryInputChanged();
            }
        }
    }

    public bool NewCategoryIsEssential
    {
        get => newCategoryIsEssential;
        set => SetProperty(ref newCategoryIsEssential, value);
    }

    public string? StatusText
    {
        get => statusText;
        private set
        {
            if (SetProperty(ref statusText, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(StatusText);

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
        currentYear = year;
        currentMonth = month;

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

    public async Task AddCategoryAsync()
    {
        var limit = ParsePositive(NewCategoryLimit);
        if (string.IsNullOrWhiteSpace(NewCategoryName) || limit is null)
        {
            StatusText = "Enter a category name and a positive monthly limit.";
            return;
        }

        var name = NewCategoryName.Trim();
        var existing = await budgetCategoryRepository.GetAsync(currentYear, currentMonth);
        if (existing.Categories.Any(category =>
                string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "A category with that name already exists this month.";
            return;
        }

        var updated = new MonthlyBudgetCategories(
            currentYear,
            currentMonth,
            [.. existing.Categories, new BudgetCategory(name, new MoneyAmount(limit.Value), NewCategoryIsEssential)]);

        await budgetCategoryRepository.SaveAsync(updated);
        await LoadMonthAsync(currentYear, currentMonth);
        ResetCategoryForm();
        StatusText = "Category added.";
    }

    public async Task DeleteCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await budgetCategoryRepository.GetAsync(currentYear, currentMonth, cancellationToken);
        var remaining = existing.Categories
            .Where(category => !string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (remaining.Length == existing.Categories.Count)
        {
            return;
        }

        var updated = new MonthlyBudgetCategories(currentYear, currentMonth, remaining);
        await budgetCategoryRepository.SaveAsync(updated, cancellationToken);
        await LoadMonthAsync(currentYear, currentMonth, cancellationToken);
    }

    private bool CanAddCategory()
    {
        return !string.IsNullOrWhiteSpace(NewCategoryName) && ParsePositive(NewCategoryLimit) is not null;
    }

    private void AfterCategoryInputChanged()
    {
        if (StatusText is not null)
        {
            StatusText = null;
        }

        ((AsyncRelayCommand)AddCategoryCommand).RaiseCanExecuteChanged();
    }

    private void ResetCategoryForm()
    {
        SetProperty(ref newCategoryName, string.Empty, nameof(NewCategoryName));
        SetProperty(ref newCategoryLimit, string.Empty, nameof(NewCategoryLimit));
        SetProperty(ref newCategoryIsEssential, false, nameof(NewCategoryIsEssential));
        ((AsyncRelayCommand)AddCategoryCommand).RaiseCanExecuteChanged();
    }

    private static decimal? ParsePositive(string value)
    {
        return decimal.TryParse(
            value,
            NumberStyles.AllowLeadingWhite
                | NumberStyles.AllowTrailingWhite
                | NumberStyles.AllowThousands
                | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var parsed) && parsed > 0m
            ? parsed
            : null;
    }
}