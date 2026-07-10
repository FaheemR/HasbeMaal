using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using HasbeMaal.Core.Application;
using HasbeMaal.Core.Import;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Presentation.ViewModels;

public sealed class SmsImportViewModel : ViewModelBase
{
    private const string PermissionBlockedMessage = "SMS access is required to import.";
    private const string ReadyToScanMessage = "Ready to scan.";

    private readonly ISmsPermissionService permissionService;
    private readonly ISmsInboxReader inboxReader;
    private readonly ISmsTransactionImporter importer;
    private readonly ITransactionApplicationService transactionApplicationService;

    private DateTimeOffset? watermark;
    private List<SmsImportReviewCandidate> pendingReview = [];

    private bool isScanning;
    private bool hasResult;
    private int readyCount;
    private int needsReviewCount;
    private int duplicateCount;
    private int ignoredCount;
    private SmsPermissionState permissionState = SmsPermissionState.Unknown;
    private SmsImportReviewFilter confidenceFilter = SmsImportReviewFilter.All;
    private string statusText = ReadyToScanMessage;

    public SmsImportViewModel(
        ISmsPermissionService permissionService,
        ISmsInboxReader inboxReader,
        ISmsTransactionImporter importer,
        ITransactionApplicationService transactionApplicationService)
    {
        ArgumentNullException.ThrowIfNull(permissionService);
        ArgumentNullException.ThrowIfNull(inboxReader);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(transactionApplicationService);

        this.permissionService = permissionService;
        this.inboxReader = inboxReader;
        this.importer = importer;
        this.transactionApplicationService = transactionApplicationService;

        ReviewGroups = [];
        ImportCommand = new AsyncRelayCommand(() => ImportAsync());
        AcceptSelectedCommand = new AsyncRelayCommand(() => AcceptSelectedAsync());
        RejectSelectedCommand = new RelayCommand(RejectSelected);
    }

    public ICommand ImportCommand { get; }

    public ICommand AcceptSelectedCommand { get; }

    public ICommand RejectSelectedCommand { get; }

    public ObservableCollection<SmsImportReviewGroupViewModel> ReviewGroups { get; }

    public bool IsScanning
    {
        get => isScanning;
        private set
        {
            if (SetProperty(ref isScanning, value))
            {
                RaiseCommandStateChanged();
            }
        }
    }

    public bool HasResult
    {
        get => hasResult;
        private set
        {
            if (SetProperty(ref hasResult, value))
            {
                OnPropertyChanged(nameof(IsReviewEmpty));
            }
        }
    }

    public int ReadyCount
    {
        get => readyCount;
        private set => SetProperty(ref readyCount, value);
    }

    public int NeedsReviewCount
    {
        get => needsReviewCount;
        private set => SetProperty(ref needsReviewCount, value);
    }

    public int DuplicateCount
    {
        get => duplicateCount;
        private set => SetProperty(ref duplicateCount, value);
    }

    public int IgnoredCount
    {
        get => ignoredCount;
        private set => SetProperty(ref ignoredCount, value);
    }

    public SmsPermissionState PermissionState
    {
        get => permissionState;
        private set
        {
            if (SetProperty(ref permissionState, value))
            {
                OnPropertyChanged(nameof(PermissionGranted));
            }
        }
    }

    public bool PermissionGranted => PermissionState == SmsPermissionState.Granted;

    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    public SmsImportReviewFilter ConfidenceFilter
    {
        get => confidenceFilter;
        set
        {
            if (SetProperty(ref confidenceFilter, value))
            {
                RebuildReviewGroups();
            }
        }
    }

    public bool IsReviewEmpty => HasResult && ReviewGroups.Count == 0;

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        if (IsScanning)
        {
            return;
        }

        PermissionState = await permissionService.GetReadPermissionStateAsync();
        if (PermissionState != SmsPermissionState.Granted)
        {
            PermissionState = await permissionService.RequestReadPermissionAsync();
            if (PermissionState != SmsPermissionState.Granted)
            {
                StatusText = PermissionBlockedMessage;
                HasResult = false;
                return;
            }
        }

        IsScanning = true;
        try
        {
            var messages = await inboxReader.ReadAsync(watermark, cancellationToken);
            var result = await importer.ImportAsync(messages, watermark, cancellationToken);

            watermark = result.Watermark;
            ReadyCount = result.Ready.Count;
            DuplicateCount = result.DuplicateCount;
            IgnoredCount = result.IgnoredCount;
            pendingReview = result.NeedsReview.ToList();
            NeedsReviewCount = pendingReview.Count;
            RebuildReviewGroups();
            HasResult = true;
            StatusText = FormatResultSummary();
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task AcceptSelectedAsync(CancellationToken cancellationToken = default)
    {
        var selected = SelectedItems();
        if (selected.Count == 0)
        {
            return;
        }

        foreach (var item in selected)
        {
            await transactionApplicationService.SaveAsync(item.Transaction, cancellationToken);
        }

        RemoveCandidates(selected.Select(item => item.Candidate));
    }

    private void RejectSelected()
    {
        var selected = SelectedItems();
        if (selected.Count == 0)
        {
            return;
        }

        RemoveCandidates(selected.Select(item => item.Candidate));
    }

    private List<SmsImportReviewItemViewModel> SelectedItems() =>
        ReviewGroups
            .SelectMany(group => group)
            .Where(item => item.IsSelected)
            .ToList();

    private void RemoveCandidates(IEnumerable<SmsImportReviewCandidate> candidates)
    {
        var removed = new HashSet<SmsImportReviewCandidate>(candidates, ReferenceEqualityComparer.Instance);
        pendingReview = pendingReview.Where(candidate => !removed.Contains(candidate)).ToList();
        NeedsReviewCount = pendingReview.Count;
        RebuildReviewGroups();
    }

    private void RebuildReviewGroups()
    {
        ReviewGroups.Clear();

        var groups = pendingReview
            .Where(MatchesFilter)
            .Select(SmsImportReviewItemViewModel.FromCandidate)
            .GroupBy(item => new
            {
                Month = new DateOnly(item.Transaction.OccurredAt.Year, item.Transaction.OccurredAt.Month, 1),
                item.Transaction.Merchant
            })
            .OrderByDescending(group => group.Key.Month)
            .ThenBy(group => group.Key.Merchant, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SmsImportReviewGroupViewModel(
                FormatTitle(group.Key.Month, group.Key.Merchant),
                group));

        foreach (var group in groups)
        {
            ReviewGroups.Add(group);
        }

        OnPropertyChanged(nameof(IsReviewEmpty));
    }

    private bool MatchesFilter(SmsImportReviewCandidate candidate) => ConfidenceFilter switch
    {
        SmsImportReviewFilter.Medium => candidate.Confidence == ParseConfidence.Medium,
        SmsImportReviewFilter.Low => candidate.Confidence == ParseConfidence.Low,
        _ => true
    };

    private string FormatResultSummary() => string.Create(
        CultureInfo.InvariantCulture,
        $"{ReadyCount} imported, {NeedsReviewCount} to review, {DuplicateCount} duplicate, {IgnoredCount} ignored.");

    private static string FormatTitle(DateOnly month, string merchant) => string.Create(
        CultureInfo.InvariantCulture,
        $"{month:yyyy MMMM} - {merchant}");

    private void RaiseCommandStateChanged()
    {
        ((AsyncRelayCommand)ImportCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)AcceptSelectedCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RejectSelectedCommand).RaiseCanExecuteChanged();
    }
}
