using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Import;
using HasbeMaal.Core.Parsing;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class SmsImportViewModelTests
{
    [TestMethod]
    public void Constructor_NullPermissionService_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SmsImportViewModel(
            null!,
            new FakeSmsInboxReader(),
            new FakeSmsTransactionImporter(),
            new CapturingTransactionApplicationService(),
            new FakeSmsImportWatermarkStore()));
    }

    [TestMethod]
    public void Constructor_NullInboxReader_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SmsImportViewModel(
            new FakeSmsPermissionService(),
            null!,
            new FakeSmsTransactionImporter(),
            new CapturingTransactionApplicationService(),
            new FakeSmsImportWatermarkStore()));
    }

    [TestMethod]
    public void Constructor_NullImporter_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SmsImportViewModel(
            new FakeSmsPermissionService(),
            new FakeSmsInboxReader(),
            null!,
            new CapturingTransactionApplicationService(),
            new FakeSmsImportWatermarkStore()));
    }

    [TestMethod]
    public void Constructor_NullTransactionApplicationService_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SmsImportViewModel(
            new FakeSmsPermissionService(),
            new FakeSmsInboxReader(),
            new FakeSmsTransactionImporter(),
            null!,
            new FakeSmsImportWatermarkStore()));
    }

    [TestMethod]
    public void Constructor_NullWatermarkStore_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SmsImportViewModel(
            new FakeSmsPermissionService(),
            new FakeSmsInboxReader(),
            new FakeSmsTransactionImporter(),
            new CapturingTransactionApplicationService(),
            null!));
    }

    [TestMethod]
    public async Task ImportAsync_PermissionDeniedThenRequestDenied_DoesNotReadOrImport()
    {
        var permissionService = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Denied,
            RequestState = SmsPermissionState.Denied
        };
        var reader = new FakeSmsInboxReader();
        var importer = new FakeSmsTransactionImporter();
        var viewModel = NewViewModel(permissionService, reader, importer);

        await viewModel.ImportAsync();

        Assert.AreEqual(1, permissionService.RequestCount);
        Assert.AreEqual(0, reader.ReadCount);
        Assert.AreEqual(0, importer.ImportCount);
        Assert.IsFalse(viewModel.HasResult);
        Assert.IsFalse(viewModel.PermissionGranted);
        Assert.AreEqual(SmsPermissionState.Denied, viewModel.PermissionState);
        Assert.AreEqual("SMS access is required to import.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task ImportAsync_PermissionGrantedFromGet_ReadsWithNullWatermarkAndPopulatesCounts()
    {
        var permissionService = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Granted
        };
        var messages = new List<SmsInboxMessage>
        {
            new("REDACTED BODY", new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero))
        };
        var reader = new FakeSmsInboxReader { Messages = messages };
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult(
                Ready: [NewTransaction("REDACTED STORE", "Groceries", 100m, Utc(2026, 7, 10))],
                NeedsReview: [NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium)],
                DuplicateCount: 2,
                IgnoredCount: 3,
                Watermark: new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero))
        };
        var viewModel = NewViewModel(permissionService, reader, importer);

        await viewModel.ImportAsync();

        Assert.AreEqual(0, permissionService.RequestCount);
        Assert.AreEqual(1, reader.ReadCount);
        Assert.IsNull(reader.LastSince);
        Assert.AreEqual(1, importer.ImportCount);
        Assert.AreSame(messages, importer.LastMessages);
        Assert.IsNull(importer.LastWatermark);
        Assert.IsTrue(viewModel.HasResult);
        Assert.AreEqual(1, viewModel.ReadyCount);
        Assert.AreEqual(1, viewModel.NeedsReviewCount);
        Assert.AreEqual(2, viewModel.DuplicateCount);
        Assert.AreEqual(3, viewModel.IgnoredCount);
        Assert.AreEqual("1 imported, 1 to review, 2 duplicate, 3 ignored.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task ImportAsync_PermissionRequestedAndGranted_ProceedsToScan()
    {
        var permissionService = new FakeSmsPermissionService
        {
            CurrentState = SmsPermissionState.Denied,
            RequestState = SmsPermissionState.Granted
        };
        var reader = new FakeSmsInboxReader();
        var importer = new FakeSmsTransactionImporter();
        var viewModel = NewViewModel(permissionService, reader, importer);

        await viewModel.ImportAsync();

        Assert.AreEqual(1, permissionService.RequestCount);
        Assert.AreEqual(1, reader.ReadCount);
        Assert.AreEqual(1, importer.ImportCount);
        Assert.IsTrue(viewModel.PermissionGranted);
        Assert.IsTrue(viewModel.HasResult);
    }

    [TestMethod]
    public async Task ImportAsync_GroupsReviewCandidatesByMonthAndMerchant()
    {
        var importer = new FakeSmsTransactionImporter
        {
            Result = ReviewOnlyResult(
                NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium),
                NewCandidate("REDACTED CAFE", "Dining", 15m, Utc(2026, 7, 2), ParseConfidence.Low),
                NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Medium),
                NewCandidate("REDACTED CLINIC", "Health", 300m, Utc(2026, 6, 30), ParseConfidence.Low))
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer);

        await viewModel.ImportAsync();

        Assert.HasCount(3, viewModel.ReviewGroups);
        Assert.AreEqual("2026 July - REDACTED CAFE", viewModel.ReviewGroups[0].Title);
        Assert.AreEqual("2026 July - REDACTED STORE", viewModel.ReviewGroups[1].Title);
        Assert.AreEqual("2026 June - REDACTED CLINIC", viewModel.ReviewGroups[2].Title);
        Assert.HasCount(2, viewModel.ReviewGroups[0]);
        Assert.IsFalse(viewModel.IsReviewEmpty);
    }

    [TestMethod]
    public async Task ImportAsync_FormatsReviewItemDisplayValues()
    {
        var importer = new FakeSmsTransactionImporter
        {
            Result = ReviewOnlyResult(
                NewCandidate("REDACTED STORE", "Groceries", 125.75m, Utc(2026, 7, 8), ParseConfidence.Medium),
                NewCandidate("REDACTED REFUND", "Groceries", 25m, Utc(2026, 7, 9), ParseConfidence.Low, TransactionDirection.Credit))
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer);

        await viewModel.ImportAsync();

        var refund = viewModel.ReviewGroups.Single(group => group.Title == "2026 July - REDACTED REFUND")[0];
        Assert.AreEqual("25.00 INR", refund.AmountText);
        Assert.AreEqual("Credit", refund.DirectionText);
        Assert.AreEqual("Low", refund.ConfidenceText);
        Assert.AreEqual(new DateOnly(2026, 7, 9), refund.OccurredOn);

        var store = viewModel.ReviewGroups.Single(group => group.Title == "2026 July - REDACTED STORE")[0];
        Assert.AreEqual("-125.75 INR", store.AmountText);
        Assert.AreEqual("Debit", store.DirectionText);
        Assert.AreEqual("Medium", store.ConfidenceText);
    }

    [TestMethod]
    public async Task ConfidenceFilter_MediumAndLow_RebuildsGroupsToMatchingConfidenceOnly()
    {
        var importer = new FakeSmsTransactionImporter
        {
            Result = ReviewOnlyResult(
                NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium),
                NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low))
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer);

        await viewModel.ImportAsync();
        Assert.HasCount(2, viewModel.ReviewGroups);

        viewModel.ConfidenceFilter = SmsImportReviewFilter.Medium;
        var mediumGroup = Assert.ContainsSingle(viewModel.ReviewGroups);
        Assert.AreEqual("2026 July - REDACTED CAFE", mediumGroup.Title);

        viewModel.ConfidenceFilter = SmsImportReviewFilter.Low;
        var lowGroup = Assert.ContainsSingle(viewModel.ReviewGroups);
        Assert.AreEqual("2026 July - REDACTED STORE", lowGroup.Title);

        viewModel.ConfidenceFilter = SmsImportReviewFilter.All;
        Assert.HasCount(2, viewModel.ReviewGroups);
    }

    [TestMethod]
    public async Task AcceptSelectedAsync_SavesSelectedTransactionsAndRemovesThem()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var accepted = NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium);
        var kept = NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low);
        var importer = new FakeSmsTransactionImporter { Result = ReviewOnlyResult(accepted, kept) };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, applicationService);

        await viewModel.ImportAsync();
        SelectItem(viewModel, "2026 July - REDACTED CAFE");

        await viewModel.AcceptSelectedAsync();

        var saved = Assert.ContainsSingle(applicationService.SavedTransactions);
        Assert.AreEqual("REDACTED CAFE", saved.Merchant);
        var remaining = Assert.ContainsSingle(viewModel.ReviewGroups);
        Assert.AreEqual("2026 July - REDACTED STORE", remaining.Title);
        Assert.AreEqual(1, viewModel.NeedsReviewCount);
    }

    [TestMethod]
    public async Task AcceptSelectedAsync_UpdatesStatusTextToReducedReviewCount()
    {
        var accepted = NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium);
        var kept = NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low);
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult(
                Ready: [NewTransaction("REDACTED SHOP", "Groceries", 100m, Utc(2026, 7, 10))],
                NeedsReview: [accepted, kept],
                DuplicateCount: 2,
                IgnoredCount: 3,
                Watermark: null)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer);

        await viewModel.ImportAsync();
        Assert.AreEqual("1 imported, 2 to review, 2 duplicate, 3 ignored.", viewModel.StatusText);

        SelectItem(viewModel, "2026 July - REDACTED CAFE");
        await viewModel.AcceptSelectedAsync();

        Assert.AreEqual(1, viewModel.NeedsReviewCount);
        Assert.AreEqual("1 imported, 1 to review, 2 duplicate, 3 ignored.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task RejectSelected_UpdatesStatusTextToReducedReviewCount()
    {
        var rejected = NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium);
        var kept = NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low);
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult(
                Ready: [],
                NeedsReview: [rejected, kept],
                DuplicateCount: 0,
                IgnoredCount: 1,
                Watermark: null)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer);

        await viewModel.ImportAsync();
        Assert.AreEqual("0 imported, 2 to review, 0 duplicate, 1 ignored.", viewModel.StatusText);

        SelectItem(viewModel, "2026 July - REDACTED CAFE");
        viewModel.RejectSelectedCommand.Execute(null);

        Assert.AreEqual(1, viewModel.NeedsReviewCount);
        Assert.AreEqual("0 imported, 1 to review, 0 duplicate, 1 ignored.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task RejectSelected_RemovesSelectedWithoutSaving()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var rejected = NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium);
        var kept = NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low);
        var importer = new FakeSmsTransactionImporter { Result = ReviewOnlyResult(rejected, kept) };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, applicationService);

        await viewModel.ImportAsync();
        SelectItem(viewModel, "2026 July - REDACTED CAFE");

        viewModel.RejectSelectedCommand.Execute(null);

        Assert.IsEmpty(applicationService.SavedTransactions);
        var remaining = Assert.ContainsSingle(viewModel.ReviewGroups);
        Assert.AreEqual("2026 July - REDACTED STORE", remaining.Title);
        Assert.AreEqual(1, viewModel.NeedsReviewCount);
    }

    [TestMethod]
    public async Task ImportAsync_SecondImport_UsesWatermarkFromFirstResult()
    {
        var permissionService = GrantedPermission();
        var reader = new FakeSmsInboxReader();
        var firstWatermark = new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult([], [], DuplicateCount: 0, IgnoredCount: 0, Watermark: firstWatermark)
        };
        var viewModel = NewViewModel(permissionService, reader, importer);

        await viewModel.ImportAsync();
        Assert.IsNull(reader.LastSince);
        Assert.IsNull(importer.LastWatermark);

        await viewModel.ImportAsync();

        Assert.AreEqual(2, reader.ReadCount);
        Assert.AreEqual(firstWatermark, reader.LastSince);
        Assert.AreEqual(firstWatermark, importer.LastWatermark);
    }

    [TestMethod]
    public async Task ImportAsync_WithReadyTransactions_EnablesUndo()
    {
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult(
                Ready: [NewTransaction("REDACTED SHOP", "Groceries", 100m, Utc(2026, 7, 10))],
                NeedsReview: [],
                DuplicateCount: 0,
                IgnoredCount: 0,
                Watermark: null)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer);

        await viewModel.ImportAsync();

        Assert.IsTrue(viewModel.CanUndo);
    }

    [TestMethod]
    public async Task UndoImportAsync_DeletesCommittedTransactionsAndDisablesUndo()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var ready = NewTransaction("REDACTED SHOP", "Groceries", 100m, Utc(2026, 7, 10));
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult(
                Ready: [ready],
                NeedsReview: [],
                DuplicateCount: 1,
                IgnoredCount: 2,
                Watermark: null)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, applicationService);

        await viewModel.ImportAsync();
        await viewModel.UndoImportAsync();

        Assert.AreEqual(ready.Id, applicationService.DeletedIds.Single());
        Assert.IsFalse(viewModel.CanUndo);
        Assert.AreEqual(0, viewModel.ReadyCount);
        Assert.AreEqual("Import undone. 1 transaction removed.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task UndoImportAsync_IncludesAcceptedReviewTransactions()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var accepted = NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium);
        var ready = NewTransaction("REDACTED SHOP", "Groceries", 100m, Utc(2026, 7, 10));
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult(
                Ready: [ready],
                NeedsReview: [accepted],
                DuplicateCount: 0,
                IgnoredCount: 0,
                Watermark: null)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, applicationService);

        await viewModel.ImportAsync();
        SelectItem(viewModel, "2026 July - REDACTED CAFE");
        await viewModel.AcceptSelectedAsync();

        await viewModel.UndoImportAsync();

        Assert.HasCount(2, applicationService.DeletedIds);
        Assert.AreEqual(ready.Id, applicationService.DeletedIds[0]);
        Assert.AreEqual(accepted.Transaction.Id, applicationService.DeletedIds[1]);
        Assert.AreEqual("Import undone. 2 transactions removed.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task UndoImportAsync_WithNothingCommitted_DoesNotDelete()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var importer = new FakeSmsTransactionImporter
        {
            Result = ReviewOnlyResult(NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low))
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, applicationService);

        await viewModel.ImportAsync();
        await viewModel.UndoImportAsync();

        Assert.IsEmpty(applicationService.DeletedIds);
        Assert.IsFalse(viewModel.CanUndo);
    }

    [TestMethod]
    public async Task ImportAsync_PersistsResultWatermarkForNextSession()
    {
        var resultWatermark = new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero);
        var watermarkStore = new FakeSmsImportWatermarkStore();
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult([], [], DuplicateCount: 0, IgnoredCount: 0, Watermark: resultWatermark)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, watermarkStore: watermarkStore);

        await viewModel.ImportAsync();

        Assert.AreEqual(resultWatermark, watermarkStore.Stored);
    }

    [TestMethod]
    public async Task ImportAsync_LoadsPersistedWatermarkOnFirstScan()
    {
        var persisted = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var watermarkStore = new FakeSmsImportWatermarkStore { Stored = persisted };
        var reader = new FakeSmsInboxReader();
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult([], [], DuplicateCount: 0, IgnoredCount: 0, Watermark: persisted)
        };
        var viewModel = NewViewModel(GrantedPermission(), reader, importer, watermarkStore: watermarkStore);

        await viewModel.ImportAsync();

        Assert.AreEqual(persisted, reader.LastSince);
        Assert.AreEqual(persisted, importer.LastWatermark);
    }

    [TestMethod]
    public async Task UndoImportAsync_RestoresPreviousWatermark()
    {
        var persisted = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var advanced = new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero);
        var watermarkStore = new FakeSmsImportWatermarkStore { Stored = persisted };
        var ready = NewTransaction("REDACTED SHOP", "Groceries", 100m, Utc(2026, 7, 10));
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult([ready], [], DuplicateCount: 0, IgnoredCount: 0, Watermark: advanced)
        };
        var viewModel = NewViewModel(GrantedPermission(), new FakeSmsInboxReader(), importer, watermarkStore: watermarkStore);

        await viewModel.ImportAsync();
        Assert.AreEqual(advanced, watermarkStore.Stored);

        await viewModel.UndoImportAsync();

        Assert.AreEqual(persisted, watermarkStore.Stored);
    }

    [TestMethod]
    public async Task ImportAsync_WhenReaderThrows_SurfacesSanitizedStatusAndStaysConsistent()
    {
        var watermarkStore = new FakeSmsImportWatermarkStore();
        var viewModel = new SmsImportViewModel(
            GrantedPermission(),
            new ThrowingSmsInboxReader(),
            new FakeSmsTransactionImporter(),
            new CapturingTransactionApplicationService(),
            watermarkStore);

        await viewModel.ImportAsync();

        Assert.AreEqual("Import failed. Try again.", viewModel.StatusText);
        Assert.IsFalse(viewModel.IsScanning);
        Assert.IsFalse(viewModel.HasResult);
        Assert.IsFalse(viewModel.CanUndo);
        Assert.IsNull(watermarkStore.Stored);
    }

    [TestMethod]
    public async Task AcceptSelectedAsync_WhenSaveThrows_KeepsCandidatesAndSurfacesSanitizedStatus()
    {
        var accepted = NewCandidate("REDACTED CAFE", "Dining", 42m, Utc(2026, 7, 9), ParseConfidence.Medium);
        var kept = NewCandidate("REDACTED STORE", "Groceries", 80m, Utc(2026, 7, 5), ParseConfidence.Low);
        var importer = new FakeSmsTransactionImporter { Result = ReviewOnlyResult(accepted, kept) };
        var viewModel = new SmsImportViewModel(
            GrantedPermission(),
            new FakeSmsInboxReader(),
            importer,
            new ThrowingTransactionApplicationService(),
            new FakeSmsImportWatermarkStore());

        await viewModel.ImportAsync();
        SelectItem(viewModel, "2026 July - REDACTED CAFE");

        await viewModel.AcceptSelectedAsync();

        Assert.AreEqual("Could not save. Try again.", viewModel.StatusText);
        Assert.HasCount(2, viewModel.ReviewGroups);
        Assert.AreEqual(2, viewModel.NeedsReviewCount);
        Assert.IsFalse(viewModel.CanUndo);
    }

    [TestMethod]
    public async Task UndoImportAsync_WhenDeleteThrows_KeepsUndoAvailableAndSurfacesSanitizedStatus()
    {
        var advanced = new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero);
        var watermarkStore = new FakeSmsImportWatermarkStore();
        var ready = NewTransaction("REDACTED SHOP", "Groceries", 100m, Utc(2026, 7, 10));
        var importer = new FakeSmsTransactionImporter
        {
            Result = new SmsImportResult([ready], [], DuplicateCount: 0, IgnoredCount: 0, Watermark: advanced)
        };
        var viewModel = new SmsImportViewModel(
            GrantedPermission(),
            new FakeSmsInboxReader(),
            importer,
            new ThrowingTransactionApplicationService(),
            watermarkStore);

        await viewModel.ImportAsync();
        Assert.IsTrue(viewModel.CanUndo);

        await viewModel.UndoImportAsync();

        Assert.AreEqual("Could not undo. Try again.", viewModel.StatusText);
        Assert.IsTrue(viewModel.CanUndo);
        Assert.AreEqual(1, viewModel.ReadyCount);
        Assert.AreEqual(advanced, watermarkStore.Stored);
    }

    private static SmsImportViewModel NewViewModel(
        FakeSmsPermissionService permissionService,
        FakeSmsInboxReader reader,
        FakeSmsTransactionImporter importer,
        CapturingTransactionApplicationService? applicationService = null,
        FakeSmsImportWatermarkStore? watermarkStore = null) =>
        new(
            permissionService,
            reader,
            importer,
            applicationService ?? new CapturingTransactionApplicationService(),
            watermarkStore ?? new FakeSmsImportWatermarkStore());

    private static FakeSmsPermissionService GrantedPermission() =>
        new() { CurrentState = SmsPermissionState.Granted };

    private static void SelectItem(SmsImportViewModel viewModel, string groupTitle)
    {
        foreach (var item in viewModel.ReviewGroups.Single(group => group.Title == groupTitle))
        {
            item.IsSelected = true;
        }
    }

    private static SmsImportResult ReviewOnlyResult(params SmsImportReviewCandidate[] candidates) =>
        new([], candidates, DuplicateCount: 0, IgnoredCount: 0, Watermark: null);

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private static SmsImportReviewCandidate NewCandidate(
        string merchant,
        string category,
        decimal amount,
        DateTimeOffset occurredAt,
        ParseConfidence confidence,
        TransactionDirection direction = TransactionDirection.Debit) =>
        new(NewTransaction(merchant, category, amount, occurredAt, direction), confidence);

    private static FinancialTransaction NewTransaction(
        string merchant,
        string category,
        decimal amount,
        DateTimeOffset occurredAt,
        TransactionDirection direction = TransactionDirection.Debit) =>
        new(
            Guid.NewGuid(),
            new MoneyAmount(amount),
            direction,
            TransactionSource.UpiSms,
            occurredAt,
            merchant,
            category,
            sourceReferenceHash: null);

    private sealed class FakeSmsPermissionService : ISmsPermissionService
    {
        public SmsPermissionState CurrentState { get; set; } = SmsPermissionState.Unknown;

        public SmsPermissionState RequestState { get; set; } = SmsPermissionState.Denied;

        public int RequestCount { get; private set; }

        public Task<SmsPermissionState> GetReadPermissionStateAsync() => Task.FromResult(CurrentState);

        public Task<SmsPermissionState> RequestReadPermissionAsync()
        {
            RequestCount++;
            CurrentState = RequestState;
            return Task.FromResult(CurrentState);
        }

        public Task OpenAppSettingsAsync() => Task.CompletedTask;
    }

    private sealed class FakeSmsInboxReader : ISmsInboxReader
    {
        public IReadOnlyList<SmsInboxMessage> Messages { get; set; } = [];

        public int ReadCount { get; private set; }

        public DateTimeOffset? LastSince { get; private set; }

        public Task<IReadOnlyList<SmsInboxMessage>> ReadAsync(
            DateTimeOffset? since,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            LastSince = since;
            return Task.FromResult(Messages);
        }
    }

    private sealed class FakeSmsTransactionImporter : ISmsTransactionImporter
    {
        public SmsImportResult Result { get; set; } = new([], [], DuplicateCount: 0, IgnoredCount: 0, Watermark: null);

        public int ImportCount { get; private set; }

        public IReadOnlyList<SmsInboxMessage>? LastMessages { get; private set; }

        public DateTimeOffset? LastWatermark { get; private set; }

        public Task<SmsImportResult> ImportAsync(
            IReadOnlyList<SmsInboxMessage> messages,
            DateTimeOffset? watermark = null,
            CancellationToken cancellationToken = default)
        {
            ImportCount++;
            LastMessages = messages;
            LastWatermark = watermark;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeSmsImportWatermarkStore : ISmsImportWatermarkStore
    {
        public DateTimeOffset? Stored { get; set; }

        public int GetCount { get; private set; }

        public List<DateTimeOffset?> SetValues { get; } = [];

        public Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(Stored);
        }

        public Task SetAsync(DateTimeOffset? watermark, CancellationToken cancellationToken = default)
        {
            Stored = watermark;
            SetValues.Add(watermark);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingTransactionApplicationService : ITransactionApplicationService
    {
        public List<FinancialTransaction> SavedTransactions { get; } = [];

        public Task<TransactionSaveResult> SaveAsync(
            FinancialTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            SavedTransactions.Add(transaction);
            return Task.FromResult(TransactionSaveResult.Saved(transaction));
        }

        public Task<IReadOnlyList<TransactionSaveResult>> SaveManyAsync(
            IReadOnlyList<FinancialTransaction> transactions,
            CancellationToken cancellationToken = default)
        {
            SavedTransactions.AddRange(transactions);
            return Task.FromResult<IReadOnlyList<TransactionSaveResult>>(
                transactions.Select(TransactionSaveResult.Saved).ToList());
        }

        public Task<FinancialTransaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<FinancialTransaction?>(null);

        public List<Guid> DeletedIds { get; } = [];

        public Task DeleteManyAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            DeletedIds.AddRange(ids);
            var targetIds = new HashSet<Guid>(ids);
            SavedTransactions.RemoveAll(transaction => targetIds.Contains(transaction.Id));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinancialTransaction>>([]);
    }

    private sealed class ThrowingSmsInboxReader : ISmsInboxReader
    {
        public Task<IReadOnlyList<SmsInboxMessage>> ReadAsync(
            DateTimeOffset? since,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("read failed");
    }

    private sealed class ThrowingTransactionApplicationService : ITransactionApplicationService
    {
        public Task<TransactionSaveResult> SaveAsync(
            FinancialTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("save failed");

        public Task<IReadOnlyList<TransactionSaveResult>> SaveManyAsync(
            IReadOnlyList<FinancialTransaction> transactions,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("save failed");

        public Task<FinancialTransaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<FinancialTransaction?>(null);

        public Task DeleteManyAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("delete failed");

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinancialTransaction>>([]);
    }
}
