using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class DashboardViewModelTests
{
    [TestMethod]
    public void Constructor_HasPredictableEmptyState()
    {
        var viewModel = NewViewModel();

        Assert.AreEqual("This month", viewModel.MonthTitle);
        Assert.AreEqual("0.00 INR", viewModel.IncomeText);
        Assert.AreEqual("0.00 INR", viewModel.ExpensesText);
        Assert.AreEqual("Surplus", viewModel.BalanceLabel);
        Assert.AreEqual("0.00 INR", viewModel.BalanceText);
        Assert.AreEqual("No activity yet", viewModel.ActivityText);
        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsFalse(viewModel.HasCategories);
        Assert.IsEmpty(viewModel.Categories);
    }

    [TestMethod]
    public async Task LoadMonthAsync_LoadsRequestedMonthSummaryFromApplicationService()
    {
        var applicationService = new ListingTransactionApplicationService(
        [
            NewTransaction("REDACTED SALARY", "Income", 500m, new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), TransactionDirection.Credit),
            NewTransaction("REDACTED STORE", "Groceries", 125.75m, new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED CLINIC", "Health", 80m, new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED STORE", "Groceries", 999m, new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero))
        ]);
        var viewModel = NewViewModel(applicationService);

        await viewModel.LoadMonthAsync(2026, 7);

        Assert.AreEqual(1, applicationService.ListCallCount);
        Assert.AreEqual(new DateOnly(2026, 7, 1), applicationService.LastFrom);
        Assert.AreEqual(new DateOnly(2026, 7, 31), applicationService.LastTo);
        Assert.AreEqual("2026 July", viewModel.MonthTitle);
        Assert.AreEqual("500.00 INR", viewModel.IncomeText);
        Assert.AreEqual("205.75 INR", viewModel.ExpensesText);
        Assert.AreEqual("Surplus", viewModel.BalanceLabel);
        Assert.AreEqual("294.25 INR", viewModel.BalanceText);
        Assert.AreEqual(3, viewModel.TransactionCount);
        Assert.AreEqual("3 transactions this month", viewModel.ActivityText);
        Assert.IsFalse(viewModel.IsEmpty);
        Assert.IsTrue(viewModel.HasCategories);
        Assert.HasCount(2, viewModel.Categories);
        Assert.AreEqual("Groceries", viewModel.Categories[0].Category);
        Assert.AreEqual("125.75 INR", viewModel.Categories[0].ExpensesText);
        Assert.AreEqual("Health", viewModel.Categories[1].Category);
        Assert.AreEqual("80.00 INR", viewModel.Categories[1].ExpensesText);
    }

    [TestMethod]
    public async Task LoadMonthAsync_NoTransactionsShowsEmptyMonthlySummary()
    {
        var viewModel = NewViewModel();

        await viewModel.LoadMonthAsync(2026, 7);

        Assert.AreEqual("2026 July", viewModel.MonthTitle);
        Assert.AreEqual("0.00 INR", viewModel.IncomeText);
        Assert.AreEqual("0.00 INR", viewModel.ExpensesText);
        Assert.AreEqual("Surplus", viewModel.BalanceLabel);
        Assert.AreEqual("0.00 INR", viewModel.BalanceText);
        Assert.AreEqual("No activity yet", viewModel.ActivityText);
        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsFalse(viewModel.HasCategories);
        Assert.IsEmpty(viewModel.Categories);
    }

    [TestMethod]
    public async Task LoadMonthAsync_ExpensesAboveIncomeShowsOverspend()
    {
        var applicationService = new ListingTransactionApplicationService(
        [
            NewTransaction("REDACTED REFUND", "Income", 200m, new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero), TransactionDirection.Credit),
            NewTransaction("REDACTED STORE", "Groceries", 700m, new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero))
        ]);
        var viewModel = NewViewModel(applicationService);

        await viewModel.LoadMonthAsync(2026, 7);

        Assert.AreEqual("Overspend", viewModel.BalanceLabel);
        Assert.AreEqual("500.00 INR", viewModel.BalanceText);
    }

    private static DashboardViewModel NewViewModel(
        ListingTransactionApplicationService? applicationService = null) =>
        new(applicationService ?? new ListingTransactionApplicationService([]));

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

        public Task DeleteManyAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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