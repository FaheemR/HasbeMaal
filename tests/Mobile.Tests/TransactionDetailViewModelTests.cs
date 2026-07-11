using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class TransactionDetailViewModelTests
{
    [TestMethod]
    public void Constructor_NullService_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TransactionDetailViewModel(null!));
    }

    [TestMethod]
    public async Task LoadAsync_ExistingTransaction_PopulatesDetailsAndReference()
    {
        var transaction = NewTransaction(
            "REDACTED STORE",
            "Groceries",
            125.75m,
            TransactionDirection.Debit,
            sourceReference: "SYNTH-UPI-REF-555",
            account: "REDACTED Bank Credit Card ••0000",
            sourceMessage: "SYNTH ORIGINAL SMS BODY XX0000");
        var viewModel = new TransactionDetailViewModel(new FakeTransactionApplicationService(transaction));

        await viewModel.LoadAsync(transaction.Id);

        Assert.IsTrue(viewModel.IsFound);
        Assert.IsFalse(viewModel.NotFound);
        Assert.IsFalse(viewModel.IsLoading);
        Assert.AreEqual("REDACTED STORE", viewModel.MerchantText);
        Assert.AreEqual("Groceries", viewModel.CategoryText);
        Assert.AreEqual("-125.75 INR", viewModel.AmountText);
        Assert.AreEqual("Debit", viewModel.DirectionText);
        Assert.AreEqual("SYNTH-UPI-REF-555", viewModel.ReferenceText);
        Assert.IsTrue(viewModel.HasReference);
        Assert.AreEqual("REDACTED Bank Credit Card ••0000", viewModel.AccountText);
        Assert.IsTrue(viewModel.HasAccount);
        Assert.AreEqual("SYNTH ORIGINAL SMS BODY XX0000", viewModel.OriginalSmsText);
        Assert.IsTrue(viewModel.HasOriginalSms);
    }

    [TestMethod]
    public async Task LoadAsync_CreditTransaction_FormatsPositiveAmount()
    {
        var transaction = NewTransaction(
            "REDACTED REFUND",
            "Groceries",
            25m,
            TransactionDirection.Credit);
        var viewModel = new TransactionDetailViewModel(new FakeTransactionApplicationService(transaction));

        await viewModel.LoadAsync(transaction.Id);

        Assert.AreEqual("25.00 INR", viewModel.AmountText);
        Assert.AreEqual("Credit", viewModel.DirectionText);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutReference_HasReferenceIsFalse()
    {
        var transaction = NewTransaction(
            "REDACTED CASH",
            "Groceries",
            50m,
            TransactionDirection.Debit);
        var viewModel = new TransactionDetailViewModel(new FakeTransactionApplicationService(transaction));

        await viewModel.LoadAsync(transaction.Id);

        Assert.IsNull(viewModel.ReferenceText);
        Assert.IsFalse(viewModel.HasReference);
    }

    [TestMethod]
    public async Task LoadAsync_MissingTransaction_SetsNotFound()
    {
        var viewModel = new TransactionDetailViewModel(new FakeTransactionApplicationService(transaction: null));

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.IsFalse(viewModel.IsFound);
        Assert.IsTrue(viewModel.NotFound);
        Assert.IsFalse(viewModel.IsLoading);
    }

    private static FinancialTransaction NewTransaction(
        string merchant,
        string category,
        decimal amount,
        TransactionDirection direction,
        string? sourceReference = null,
        string? account = null,
        string? sourceMessage = null)
    {
        return new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(amount),
            direction,
            TransactionSource.UpiSms,
            new DateTimeOffset(2026, 7, 8, 10, 15, 0, TimeSpan.Zero),
            merchant,
            category,
            sourceReferenceHash: null,
            sourceReference: sourceReference,
            account: account,
            sourceMessage: sourceMessage);
    }

    private sealed class FakeTransactionApplicationService : ITransactionApplicationService
    {
        private readonly FinancialTransaction? transaction;

        public FakeTransactionApplicationService(FinancialTransaction? transaction)
        {
            this.transaction = transaction;
        }

        public Task<FinancialTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(transaction is not null && transaction.Id == id ? transaction : null);
        }

        public Task<TransactionSaveResult> SaveAsync(
            FinancialTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TransactionSaveResult>> SaveManyAsync(
            IReadOnlyList<FinancialTransaction> transactions,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteManyAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
