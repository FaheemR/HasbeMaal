using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class BudgetsViewModelTests
{
    [TestMethod]
    public void Constructor_HasPredictableEmptyState()
    {
        var viewModel = NewViewModel();

        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsFalse(viewModel.IsLoading);
        Assert.IsEmpty(viewModel.Items);
    }

    [TestMethod]
    public async Task LoadMonthAsync_LoadsPersistedBudgetProgressFromMonthlySpending()
    {
        var budgetCategories = new MonthlyBudgetCategories(
            2026,
            7,
            [
                new BudgetCategory("Groceries", new MoneyAmount(500m), isEssential: true),
                new BudgetCategory("Transport", new MoneyAmount(200m), isEssential: false)
            ]);
        var budgetRepository = new RecordingMonthlyBudgetCategoryRepository(budgetCategories);
        var applicationService = new ListingTransactionApplicationService(
        [
            NewTransaction("REDACTED STORE", "Groceries", 125.75m, new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED TAXI", "Transport", 80m, new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED REFUND", "Groceries", 25m, new DateTimeOffset(2026, 7, 12, 0, 0, 0, TimeSpan.Zero), TransactionDirection.Credit),
            NewTransaction("REDACTED STORE", "Groceries", 999m, new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero))
        ]);
        var viewModel = NewViewModel(budgetRepository, applicationService);

        await viewModel.LoadMonthAsync(2026, 7);

        Assert.AreEqual(1, budgetRepository.GetCallCount);
        Assert.AreEqual(2026, budgetRepository.LastYear);
        Assert.AreEqual(7, budgetRepository.LastMonth);
        Assert.AreEqual(1, applicationService.ListCallCount);
        Assert.AreEqual(new DateOnly(2026, 7, 1), applicationService.LastFrom);
        Assert.AreEqual(new DateOnly(2026, 7, 31), applicationService.LastTo);
        Assert.IsFalse(viewModel.IsLoading);
        Assert.IsFalse(viewModel.IsEmpty);
        Assert.HasCount(2, viewModel.Items);
        Assert.AreEqual("Groceries", viewModel.Items[0].CategoryName);
        Assert.AreEqual("Spent 125.75 INR", viewModel.Items[0].SpentText);
        Assert.AreEqual("25.2% used", viewModel.Items[0].RemainingText);
        Assert.AreEqual("374.25 INR remaining", viewModel.Items[0].StatusText);
        Assert.AreEqual("Transport", viewModel.Items[1].CategoryName);
        Assert.AreEqual("Spent 80.00 INR", viewModel.Items[1].SpentText);
    }

    [TestMethod]
    public async Task LoadMonthAsync_BudgetCategoryWithoutSpendingShowsZeroSpent()
    {
        var budgetCategories = new MonthlyBudgetCategories(
            2026,
            7,
            [new BudgetCategory("Education", new MoneyAmount(1500m), isEssential: true)]);
        var viewModel = NewViewModel(new RecordingMonthlyBudgetCategoryRepository(budgetCategories));

        await viewModel.LoadMonthAsync(2026, 7);

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Spent 0.00 INR", item.SpentText);
        Assert.AreEqual("0% used", item.RemainingText);
        Assert.AreEqual("1500.00 INR remaining", item.StatusText);
    }

    [TestMethod]
    public void Load_BudgetUnderLimit_UsesBudgetEvaluationForLabels()
    {
        var viewModel = NewViewModel();
        var category = new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true);

        viewModel.Load([(category, new MoneyAmount(250m))]);

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Groceries", item.CategoryName);
        Assert.AreEqual("Essential", item.CategoryTypeText);
        Assert.AreEqual("Spent 250.00 INR", item.SpentText);
        Assert.AreEqual("Limit 1000.00 INR", item.LimitText);
        Assert.AreEqual("25% used", item.RemainingText);
        Assert.AreEqual("750.00 INR remaining", item.StatusText);
        Assert.AreEqual(0.25m, item.Progress);
    }

    [TestMethod]
    public void Load_BudgetOverLimit_UsesFactualStatus()
    {
        var viewModel = NewViewModel();
        var category = new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false);

        viewModel.Load([(category, new MoneyAmount(650m))]);

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Flexible", item.CategoryTypeText);
        Assert.AreEqual("130% used", item.RemainingText);
        Assert.AreEqual("Over limit by 150.00 INR", item.StatusText);
        Assert.AreEqual(1m, item.Progress);
    }

    [TestMethod]
    public void Load_BudgetProgressCopy_DoesNotUseAdviceWords()
    {
        var viewModel = NewViewModel();
        var category = new BudgetCategory("Education", new MoneyAmount(1000m), isEssential: true);
        string[] adviceWords = ["recommend", "should", "must", "advice", "warning", "improve"];

        viewModel.Load([(category, new MoneyAmount(1000m))]);

        var item = Assert.ContainsSingle(viewModel.Items);
        var copy = string.Join(
            " ",
            item.CategoryName,
            item.CategoryTypeText,
            item.SpentText,
            item.LimitText,
            item.RemainingText,
            item.StatusText);

        foreach (var adviceWord in adviceWords)
        {
            Assert.DoesNotContain(adviceWord, copy, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static BudgetsViewModel NewViewModel(
        RecordingMonthlyBudgetCategoryRepository? budgetRepository = null,
        ListingTransactionApplicationService? applicationService = null) =>
        new(
            budgetRepository ?? new RecordingMonthlyBudgetCategoryRepository(MonthlyBudgetCategories.Empty(2026, 7)),
            applicationService ?? new ListingTransactionApplicationService([]));

    private static FinancialTransaction NewTransaction(
        string merchant,
        string category,
        decimal amount,
        DateTimeOffset occurredAt,
        TransactionDirection direction = TransactionDirection.Debit)
    {
        return new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(amount),
            direction,
            TransactionSource.ManualCash,
            occurredAt,
            merchant,
            category,
            sourceReferenceHash: null);
    }

    private sealed class RecordingMonthlyBudgetCategoryRepository : IMonthlyBudgetCategoryRepository
    {
        private readonly MonthlyBudgetCategories budgetCategories;

        public RecordingMonthlyBudgetCategoryRepository(MonthlyBudgetCategories budgetCategories)
        {
            this.budgetCategories = budgetCategories;
        }

        public int GetCallCount { get; private set; }

        public int LastYear { get; private set; }

        public int LastMonth { get; private set; }

        public Task SaveAsync(
            MonthlyBudgetCategories budgetCategories,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<MonthlyBudgetCategories> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            LastYear = year;
            LastMonth = month;

            return Task.FromResult(budgetCategories);
        }
    }

    private sealed class ListingTransactionApplicationService : ITransactionApplicationService
    {
        private readonly IReadOnlyList<FinancialTransaction> transactions;

        public ListingTransactionApplicationService(IReadOnlyList<FinancialTransaction> transactions)
        {
            this.transactions = transactions;
        }

        public int ListCallCount { get; private set; }

        public DateOnly LastFrom { get; private set; }

        public DateOnly LastTo { get; private set; }

        public Task<TransactionSaveResult> SaveAsync(
            FinancialTransaction transaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(TransactionSaveResult.Saved(transaction));

        public Task<FinancialTransaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(transactions.SingleOrDefault(transaction => transaction.Id == id));

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            LastFrom = from;
            LastTo = to;

            return Task.FromResult(transactions);
        }
    }
}