using System.Collections.ObjectModel;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class BudgetsViewModel : ViewModelBase
{
    public BudgetsViewModel()
    {
        Items = [];
    }

    public ObservableCollection<BudgetProgressItemViewModel> Items { get; }

    public bool IsEmpty => Items.Count == 0;

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