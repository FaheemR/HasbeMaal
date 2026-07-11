using HasbeMaal.Core.Import;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class BankSelectionViewModelTests
{
    [TestMethod]
    public void Constructor_NullStore_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BankSelectionViewModel(null!));
    }

    [TestMethod]
    public async Task LoadAsync_NoStoredSelection_SelectsAllBanks()
    {
        var store = new FakeSelectedBanksStore();
        var viewModel = new BankSelectionViewModel(store);

        await viewModel.LoadAsync();

        Assert.HasCount(BankSenderRegistry.Banks.Count, viewModel.Banks);
        Assert.AreEqual(BankSenderRegistry.Banks.Count, viewModel.SelectedCount);
        Assert.IsTrue(viewModel.Banks.All(bank => bank.IsSelected));
        Assert.IsTrue(viewModel.CanSave);
    }

    [TestMethod]
    public async Task LoadAsync_StoredSelection_SelectsOnlyStoredBanks()
    {
        var store = new FakeSelectedBanksStore { Stored = ["jkbank", "hdfc"] };
        var viewModel = new BankSelectionViewModel(store);

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.SelectedCount);
        Assert.IsTrue(viewModel.Banks.Single(bank => bank.Id == "jkbank").IsSelected);
        Assert.IsTrue(viewModel.Banks.Single(bank => bank.Id == "hdfc").IsSelected);
        Assert.IsFalse(viewModel.Banks.Single(bank => bank.Id == "sbi").IsSelected);
    }

    [TestMethod]
    public async Task SaveAsync_PersistsSelectedBankIds()
    {
        var store = new FakeSelectedBanksStore { Stored = ["jkbank", "hdfc"] };
        var viewModel = new BankSelectionViewModel(store);
        await viewModel.LoadAsync();

        await viewModel.SaveAsync();

        var saved = Assert.ContainsSingle(store.SetValues);
        Assert.HasCount(2, saved);
        Assert.IsTrue(saved.Contains("jkbank"));
        Assert.IsTrue(saved.Contains("hdfc"));
    }

    [TestMethod]
    public async Task SaveAsync_AllSelected_PersistsAllIdsAndReportsAllBanks()
    {
        var store = new FakeSelectedBanksStore();
        var viewModel = new BankSelectionViewModel(store);
        await viewModel.LoadAsync();

        await viewModel.SaveAsync();

        var saved = Assert.ContainsSingle(store.SetValues);
        Assert.HasCount(BankSenderRegistry.Banks.Count, saved);
        Assert.AreEqual("Saved. Scanning all banks.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task ClearAll_ThenSave_CannotSaveAndDoesNotPersist()
    {
        var store = new FakeSelectedBanksStore();
        var viewModel = new BankSelectionViewModel(store);
        await viewModel.LoadAsync();

        viewModel.ClearAllCommand.Execute(null);

        Assert.AreEqual(0, viewModel.SelectedCount);
        Assert.IsFalse(viewModel.CanSave);
        Assert.AreEqual("Select at least one bank to scan.", viewModel.StatusText);

        await viewModel.SaveAsync();

        Assert.IsEmpty(store.SetValues);
    }

    [TestMethod]
    public async Task SelectAll_AfterClear_ReselectsEveryBank()
    {
        var store = new FakeSelectedBanksStore { Stored = ["jkbank"] };
        var viewModel = new BankSelectionViewModel(store);
        await viewModel.LoadAsync();

        viewModel.ClearAllCommand.Execute(null);
        viewModel.SelectAllCommand.Execute(null);

        Assert.AreEqual(BankSenderRegistry.Banks.Count, viewModel.SelectedCount);
        Assert.IsTrue(viewModel.CanSave);
    }

    [TestMethod]
    public async Task SaveAsync_Subset_ReportsNarrowedScan()
    {
        var store = new FakeSelectedBanksStore();
        var viewModel = new BankSelectionViewModel(store);
        await viewModel.LoadAsync();
        viewModel.ClearAllCommand.Execute(null);
        viewModel.Banks.Single(bank => bank.Id == "jkbank").IsSelected = true;
        viewModel.Banks.Single(bank => bank.Id == "hdfc").IsSelected = true;

        await viewModel.SaveAsync();

        var saved = Assert.ContainsSingle(store.SetValues);
        Assert.HasCount(2, saved);
        Assert.AreEqual($"Saved. Scanning 2 of {BankSenderRegistry.Banks.Count} banks.", viewModel.StatusText);
    }

    private sealed class FakeSelectedBanksStore : ISelectedBanksStore
    {
        public IReadOnlyList<string> Stored { get; set; } = Array.Empty<string>();

        public List<IReadOnlyList<string>> SetValues { get; } = [];

        public Task<IReadOnlyList<string>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored);
        }

        public Task SetAsync(IReadOnlyList<string> bankIds, CancellationToken cancellationToken = default)
        {
            Stored = bankIds;
            SetValues.Add(bankIds);
            return Task.CompletedTask;
        }
    }
}
