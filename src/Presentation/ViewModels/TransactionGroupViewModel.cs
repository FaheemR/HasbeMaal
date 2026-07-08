namespace HasbeMaal.Presentation.ViewModels;

public sealed class TransactionGroupViewModel : List<TransactionListItemViewModel>
{
    public TransactionGroupViewModel(string title, IEnumerable<TransactionListItemViewModel> transactions)
        : base(transactions)
    {
        Title = title;
    }

    public string Title { get; }
}