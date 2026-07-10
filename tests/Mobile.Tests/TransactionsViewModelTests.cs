using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class TransactionsViewModelTests
{
    [TestMethod]
    public void Load_EmptyTransactions_SetsEmptyState()
    {
        var viewModel = NewViewModel();

        viewModel.Load([]);

        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsFalse(viewModel.IsLoading);
        Assert.IsEmpty(viewModel.Groups);
    }

    [TestMethod]
    public void Load_GroupsTransactionsByMonthAndCategory()
    {
        var viewModel = NewViewModel();
        var transactions = new[]
        {
            NewTransaction("REDACTED SCHOOL", "Education", 450m, new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED STORE", "Groceries", 125.75m, new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED STORE", "Groceries", 80m, new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED CLINIC", "Health", 300m, new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero))
        };

        viewModel.Load(transactions);

        Assert.IsFalse(viewModel.IsEmpty);
        Assert.HasCount(3, viewModel.Groups);
        Assert.AreEqual("2026 July - Education", viewModel.Groups[0].Title);
        Assert.AreEqual("2026 July - Groceries", viewModel.Groups[1].Title);
        Assert.AreEqual("2026 June - Health", viewModel.Groups[2].Title);
        Assert.HasCount(2, viewModel.Groups[1]);
    }

    [TestMethod]
    public void Load_FormatsDebitAndCreditAmounts()
    {
        var viewModel = NewViewModel();
        var transactions = new[]
        {
            NewTransaction("REDACTED STORE", "Groceries", 125.75m, new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero)),
            NewTransaction("REDACTED REFUND", "Groceries", 25m, new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero), TransactionDirection.Credit)
        };

        viewModel.Load(transactions);

        Assert.AreEqual("25.00 INR", viewModel.Groups[0][0].AmountText);
        Assert.AreEqual("Credit", viewModel.Groups[0][0].DirectionText);
        Assert.AreEqual("-125.75 INR", viewModel.Groups[0][1].AmountText);
        Assert.AreEqual("Debit", viewModel.Groups[0][1].DirectionText);
    }

    [TestMethod]
    public async Task LoadAsync_LoadsTransactionsFromApplicationService()
    {
        var transaction = NewTransaction(
            "REDACTED STORE",
            "Groceries",
            125.75m,
            new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero));
        var applicationService = new ListingTransactionApplicationService([transaction]);
        var viewModel = NewViewModel(applicationService);

        await viewModel.LoadAsync();

        Assert.AreEqual(1, applicationService.ListCallCount);
        Assert.AreEqual(DateOnly.MinValue, applicationService.LastFrom);
        Assert.AreEqual(DateOnly.MaxValue, applicationService.LastTo);
        Assert.IsFalse(viewModel.IsEmpty);
        var group = Assert.ContainsSingle(viewModel.Groups);
        var item = Assert.ContainsSingle(group);
        Assert.AreEqual("REDACTED STORE", item.Merchant);
    }

    private static TransactionsViewModel NewViewModel(
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

        public Task<IReadOnlyList<TransactionSaveResult>> SaveManyAsync(
            IReadOnlyList<FinancialTransaction> transactions,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TransactionSaveResult>>(
                transactions.Select(TransactionSaveResult.Saved).ToList());

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