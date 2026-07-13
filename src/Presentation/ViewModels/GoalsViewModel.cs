using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class GoalsViewModel : ViewModelBase
{
    private const string EmergencyFundName = "Emergency fund";

    private readonly IFinancialGoalRepository goalRepository;

    private bool isLoading;
    private string newGoalName = string.Empty;
    private string newGoalPurpose = string.Empty;
    private string newGoalTargetAmount = string.Empty;
    private string newGoalCurrentAmount = string.Empty;
    private DateTime newGoalTargetDate = DateTime.Today.AddMonths(6);
    private string? statusText;

    public GoalsViewModel(IFinancialGoalRepository goalRepository)
    {
        ArgumentNullException.ThrowIfNull(goalRepository);

        this.goalRepository = goalRepository;
        Items = [];
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
        AddGoalCommand = new AsyncRelayCommand(AddGoalAsync, CanAddGoal);
    }

    public ObservableCollection<GoalItemViewModel> Items { get; }

    public ICommand RefreshCommand { get; }

    public ICommand AddGoalCommand { get; }

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

    public string NewGoalName
    {
        get => newGoalName;
        set
        {
            if (SetProperty(ref newGoalName, value))
            {
                AfterInputChanged();
            }
        }
    }

    public string NewGoalPurpose
    {
        get => newGoalPurpose;
        set => SetProperty(ref newGoalPurpose, value);
    }

    public string NewGoalTargetAmount
    {
        get => newGoalTargetAmount;
        set
        {
            if (SetProperty(ref newGoalTargetAmount, value))
            {
                AfterInputChanged();
            }
        }
    }

    public string NewGoalCurrentAmount
    {
        get => newGoalCurrentAmount;
        set
        {
            if (SetProperty(ref newGoalCurrentAmount, value))
            {
                AfterInputChanged();
            }
        }
    }

    public DateTime NewGoalTargetDate
    {
        get => newGoalTargetDate;
        set => SetProperty(ref newGoalTargetDate, value.Date);
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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            var goals = await goalRepository.ListAsync(cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.Today);

            Items.Clear();
            foreach (var goal in goals.OrderBy(goal => goal.TargetDate))
            {
                Items.Add(GoalItemViewModel.FromGoal(goal, today));
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public async Task AddEmergencyFundAsync(
        decimal essentialMonthlyExpenses,
        int monthsOfCover,
        decimal currentSaved,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        if (essentialMonthlyExpenses <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(essentialMonthlyExpenses));
        }

        if (monthsOfCover <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthsOfCover));
        }

        if (currentSaved < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(currentSaved));
        }

        var target = essentialMonthlyExpenses * monthsOfCover;
        var goal = new FinancialGoal(
            EmergencyFundName,
            new MoneyAmount(target),
            new MoneyAmount(currentSaved),
            targetDate,
            $"{monthsOfCover} months of essential expenses");

        await goalRepository.SaveAsync(goal, cancellationToken);
        await LoadAsync(cancellationToken);
    }

    public async Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await goalRepository.DeleteAsync(id, cancellationToken);
        await LoadAsync(cancellationToken);
    }

    public async Task UpdateGoalSavedAmountAsync(
        Guid id,
        decimal savedAmount,
        CancellationToken cancellationToken = default)
    {
        if (savedAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(savedAmount));
        }

        var goals = await goalRepository.ListAsync(cancellationToken);
        var existing = goals.FirstOrDefault(goal => goal.Id == id);
        if (existing is null)
        {
            return;
        }

        var updated = new FinancialGoal(
            existing.Id,
            existing.Name,
            existing.TargetAmount,
            new MoneyAmount(savedAmount, existing.TargetAmount.Currency),
            existing.TargetDate,
            existing.Purpose);

        await goalRepository.SaveAsync(updated, cancellationToken);
        await LoadAsync(cancellationToken);
    }

    private bool CanAddGoal()
    {
        return !string.IsNullOrWhiteSpace(NewGoalName)
            && ParsePositive(NewGoalTargetAmount) is not null
            && ParseCurrent(NewGoalCurrentAmount) is not null;
    }

    private async Task AddGoalAsync()
    {
        var target = ParsePositive(NewGoalTargetAmount);
        var current = ParseCurrent(NewGoalCurrentAmount);

        if (string.IsNullOrWhiteSpace(NewGoalName) || target is null || current is null)
        {
            StatusText = "Enter a name and a positive target amount.";
            return;
        }

        if (current.Value > target.Value)
        {
            StatusText = "Saved amount cannot be more than the target.";
            return;
        }

        var purpose = string.IsNullOrWhiteSpace(NewGoalPurpose)
            ? NewGoalName.Trim()
            : NewGoalPurpose.Trim();

        var goal = new FinancialGoal(
            NewGoalName.Trim(),
            new MoneyAmount(target.Value),
            new MoneyAmount(current.Value),
            DateOnly.FromDateTime(NewGoalTargetDate.Date),
            purpose);

        await goalRepository.SaveAsync(goal);
        await LoadAsync();
        ResetForm();
        StatusText = "Goal saved.";
    }

    private void AfterInputChanged()
    {
        if (StatusText is not null)
        {
            StatusText = null;
        }

        ((AsyncRelayCommand)AddGoalCommand).RaiseCanExecuteChanged();
    }

    private void ResetForm()
    {
        SetProperty(ref newGoalName, string.Empty, nameof(NewGoalName));
        SetProperty(ref newGoalPurpose, string.Empty, nameof(NewGoalPurpose));
        SetProperty(ref newGoalTargetAmount, string.Empty, nameof(NewGoalTargetAmount));
        SetProperty(ref newGoalCurrentAmount, string.Empty, nameof(NewGoalCurrentAmount));
        SetProperty(ref newGoalTargetDate, DateTime.Today.AddMonths(6), nameof(NewGoalTargetDate));
        ((AsyncRelayCommand)AddGoalCommand).RaiseCanExecuteChanged();
    }

    private static decimal? ParsePositive(string value)
    {
        return Parse(value) is { } parsed && parsed > 0m ? parsed : null;
    }

    private static decimal? ParseCurrent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        return Parse(value) is { } parsed && parsed >= 0m ? parsed : null;
    }

    private static decimal? Parse(string value)
    {
        return decimal.TryParse(
            value,
            NumberStyles.AllowLeadingWhite
                | NumberStyles.AllowTrailingWhite
                | NumberStyles.AllowThousands
                | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }
}
