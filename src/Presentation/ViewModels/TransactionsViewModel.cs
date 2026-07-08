using System.Collections.ObjectModel;
using System.Globalization;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class TransactionsViewModel : ViewModelBase
{
    private bool isLoading;

    public TransactionsViewModel()
    {
        Groups = [];
    }

    public ObservableCollection<TransactionGroupViewModel> Groups { get; }

    public bool IsEmpty => !IsLoading && Groups.Count == 0;

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

    public void Load(IEnumerable<FinancialTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        IsLoading = true;
        Groups.Clear();

        var groups = transactions
            .OrderByDescending(transaction => transaction.OccurredAt)
            .ThenBy(transaction => transaction.Category, StringComparer.OrdinalIgnoreCase)
            .GroupBy(transaction => new
            {
                Month = new DateOnly(transaction.OccurredAt.Year, transaction.OccurredAt.Month, 1),
                transaction.Category
            })
            .OrderByDescending(group => group.Key.Month)
            .ThenBy(group => group.Key.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TransactionGroupViewModel(
                FormatTitle(group.Key.Month, group.Key.Category),
                group.Select(TransactionListItemViewModel.FromTransaction)));

        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        IsLoading = false;
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static string FormatTitle(DateOnly month, string category)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{month:yyyy MMMM} - {category}");
    }
}