namespace HasbeMaal.Presentation.ViewModels;

/// <summary>
/// A selectable bank or UPI app row in the SMS bank-selection screen. Wraps a curated registry
/// entry (see <c>BankSenderRegistry</c>) with a toggle. <see cref="Id"/> and <see cref="Name"/> are
/// PUBLIC registry values, never user data.
/// </summary>
public sealed class BankSelectionItemViewModel : ViewModelBase
{
    private bool isSelected;

    public BankSelectionItemViewModel(string id, string name, bool isSelected)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        this.isSelected = isSelected;
    }

    /// <summary>Stable registry id persisted when the user saves a selection.</summary>
    public string Id { get; }

    /// <summary>Display name shown in the selection list.</summary>
    public string Name { get; }

    /// <summary>Whether this bank is currently selected as an SMS transaction source.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
