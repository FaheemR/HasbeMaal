using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class ManualTransactionEntryViewModelTests
{
    [TestMethod]
    public void Constructor_DefaultState_IsInvalidUntilRequiredFieldsAreEntered()
    {
        var viewModel = new ManualTransactionEntryViewModel(new CapturingTransactionApplicationService());

        Assert.IsTrue(viewModel.HasErrors);
        Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        Assert.IsNotNull(viewModel.AmountError);
        Assert.IsNotNull(viewModel.MerchantError);
        Assert.IsNull(viewModel.CategoryError);
    }

    [TestMethod]
    public void Validate_PositiveInvariantDecimalAndMerchantAndCategory_IsValid()
    {
        var viewModel = NewValidViewModel();

        var isValid = viewModel.Validate();

        Assert.IsTrue(isValid);
        Assert.IsFalse(viewModel.HasErrors);
        Assert.IsTrue(viewModel.SaveCommand.CanExecute(null));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("abc")]
    [DataRow("1,23")]
    [DataRow("1,234.56")]
    [DataRow("125,75")]
    [DataRow("")]
    public void Validate_InvalidAmount_IsInvalid(string amount)
    {
        var viewModel = NewValidViewModel();
        viewModel.Amount = amount;

        Assert.IsFalse(viewModel.Validate());
        Assert.AreEqual("Enter a positive amount.", viewModel.AmountError);
        Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task SaveCommand_ValidDebit_PersistsManualCashTransaction()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var viewModel = NewValidViewModel(applicationService);

        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync();

        Assert.IsNotNull(viewModel.LastCreatedTransaction);
        var transaction = viewModel.LastCreatedTransaction;
        Assert.AreEqual(125.75m, transaction.Amount.Amount);
        Assert.AreEqual("INR", transaction.Amount.Currency);
        Assert.AreEqual(TransactionDirection.Debit, transaction.Direction);
        Assert.AreEqual(TransactionSource.ManualCash, transaction.Source);
        Assert.AreEqual("REDACTED STORE", transaction.Merchant);
        Assert.AreEqual("Groceries", transaction.Category);
        Assert.IsNull(transaction.SourceReferenceHash);
        Assert.AreEqual(1, applicationService.SaveCallCount);
        Assert.AreSame(transaction, applicationService.SavedTransactions.Single());
    }

    [TestMethod]
    public async Task SaveCommand_CreditToggle_CreatesCreditTransaction()
    {
        var viewModel = NewValidViewModel();
        viewModel.IsCredit = true;

        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync();

        Assert.IsNotNull(viewModel.LastCreatedTransaction);
        var transaction = viewModel.LastCreatedTransaction;
        Assert.AreEqual(TransactionDirection.Credit, transaction.Direction);
    }

    [TestMethod]
    public async Task SaveCommand_InvalidInput_DoesNotCreateOrPersistTransaction()
    {
        var applicationService = new CapturingTransactionApplicationService();
        var viewModel = NewValidViewModel(applicationService);
        viewModel.Amount = "0";

        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync();

        Assert.IsNull(viewModel.LastCreatedTransaction);
        Assert.AreEqual(0, applicationService.SaveCallCount);
    }

    private static ManualTransactionEntryViewModel NewValidViewModel(
        CapturingTransactionApplicationService? applicationService = null)
    {
        return new ManualTransactionEntryViewModel(applicationService ?? new CapturingTransactionApplicationService())
        {
            Amount = "125.75",
            Merchant = "REDACTED STORE",
            Category = "Groceries",
            OccurredOn = new DateTime(2026, 7, 8)
        };
    }

    private sealed class CapturingTransactionApplicationService : ITransactionApplicationService
    {
        private readonly List<FinancialTransaction> savedTransactions = [];

        public int SaveCallCount { get; private set; }

        public IReadOnlyList<FinancialTransaction> SavedTransactions => savedTransactions;

        public Task<TransactionSaveResult> SaveAsync(
            FinancialTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            savedTransactions.Add(transaction);

            return Task.FromResult(TransactionSaveResult.Saved(transaction));
        }

        public Task<FinancialTransaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(savedTransactions.SingleOrDefault(transaction => transaction.Id == id));

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinancialTransaction>>(savedTransactions);
    }
}