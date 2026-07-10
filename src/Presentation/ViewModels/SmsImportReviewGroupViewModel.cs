namespace HasbeMaal.Presentation.ViewModels;

public sealed class SmsImportReviewGroupViewModel : List<SmsImportReviewItemViewModel>
{
    public SmsImportReviewGroupViewModel(string title, IEnumerable<SmsImportReviewItemViewModel> items)
        : base(items)
    {
        Title = title;
    }

    public string Title { get; }
}
