using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using HasbeMaal.Core.Import;

namespace HasbeMaal.Presentation.ViewModels;

/// <summary>
/// Presents the curated bank/UPI registry as a multi-select list and persists the user's choice via
/// <see cref="ISelectedBanksStore"/>. Before any selection exists the full registry is shown as
/// selected, matching the "default = scan everything" behavior. Only PUBLIC registry ids are stored;
/// no message content, sender address, or reference is ever handled here.
/// </summary>
public sealed class BankSelectionViewModel : ViewModelBase
{
    private readonly ISelectedBanksStore selectedBanksStore;
    private readonly AsyncRelayCommand saveCommand;
    private readonly RelayCommand selectAllCommand;
    private readonly RelayCommand clearAllCommand;

    private bool isBusy;
    private bool isLoaded;
    private string statusText = "Choose which banks and UPI apps to scan.";

    public BankSelectionViewModel(ISelectedBanksStore selectedBanksStore)
    {
        this.selectedBanksStore = selectedBanksStore ?? throw new ArgumentNullException(nameof(selectedBanksStore));

        Banks = new ObservableCollection<BankSelectionItemViewModel>();
        saveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        selectAllCommand = new RelayCommand(SelectAll, () => !IsBusy);
        clearAllCommand = new RelayCommand(ClearAll, () => !IsBusy);
    }

    /// <summary>The selectable banks and UPI apps, in registry order.</summary>
    public ObservableCollection<BankSelectionItemViewModel> Banks { get; }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanSave));
                RaiseCommandStates();
            }
        }
    }

    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    /// <summary>Number of banks currently selected.</summary>
    public int SelectedCount => Banks.Count(bank => bank.IsSelected);

    /// <summary>True when at least one bank is selected and no save is in progress.</summary>
    public bool CanSave => !IsBusy && SelectedCount > 0;

    public ICommand SaveCommand => saveCommand;

    public ICommand SelectAllCommand => selectAllCommand;

    public ICommand ClearAllCommand => clearAllCommand;

    /// <summary>
    /// Loads the registry and applies the persisted selection. An empty stored selection means
    /// "no explicit choice yet", so every bank is shown as selected (the scan-everything default).
    /// </summary>
    public async Task LoadAsync()
    {
        if (isLoaded || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var storedIds = await selectedBanksStore.GetAsync();
            var selectedIds = new HashSet<string>(storedIds, StringComparer.OrdinalIgnoreCase);
            var selectAll = selectedIds.Count == 0;

            foreach (var item in Banks)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }

            Banks.Clear();
            foreach (var bank in BankSenderRegistry.Banks)
            {
                var isSelected = selectAll || selectedIds.Contains(bank.Id);
                var item = new BankSelectionItemViewModel(bank.Id, bank.Name, isSelected);
                item.PropertyChanged += OnItemPropertyChanged;
                Banks.Add(item);
            }

            isLoaded = true;
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(CanSave));
            RaiseCommandStates();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Persists the currently selected bank ids.</summary>
    public async Task SaveAsync()
    {
        if (!CanSave)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Saving selection…";
        try
        {
            var selectedIds = Banks.Where(bank => bank.IsSelected).Select(bank => bank.Id).ToArray();
            await selectedBanksStore.SetAsync(selectedIds);

            StatusText = selectedIds.Length == Banks.Count
                ? "Saved. Scanning all banks."
                : $"Saved. Scanning {selectedIds.Length} of {Banks.Count} banks.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SelectAll() => SetAllSelected(true);

    private void ClearAll() => SetAllSelected(false);

    private void SetAllSelected(bool value)
    {
        foreach (var item in Banks)
        {
            item.IsSelected = value;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BankSelectionItemViewModel.IsSelected))
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanSave));
        RaiseCommandStates();

        if (SelectedCount == 0)
        {
            StatusText = "Select at least one bank to scan.";
        }
    }

    private void RaiseCommandStates()
    {
        saveCommand.RaiseCanExecuteChanged();
        selectAllCommand.RaiseCanExecuteChanged();
        clearAllCommand.RaiseCanExecuteChanged();
    }
}
