using HasbeMaal.Core.Domain;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class ManualTransactionEntryViewModelTests
{
    [TestMethod]
    public void Constructor_DefaultState_IsInvalidUntilRequiredFieldsAreEntered()
    {
        var viewModel = new ManualTransactionEntryViewModel();

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
    public void SaveCommand_ValidDebit_CreatesManualCashTransaction()
    {
        var viewModel = NewValidViewModel();

        viewModel.SaveCommand.Execute(null);

        Assert.IsNotNull(viewModel.LastCreatedTransaction);
        var transaction = viewModel.LastCreatedTransaction;
        Assert.AreEqual(125.75m, transaction.Amount.Amount);
        Assert.AreEqual("INR", transaction.Amount.Currency);
        Assert.AreEqual(TransactionDirection.Debit, transaction.Direction);
        Assert.AreEqual(TransactionSource.ManualCash, transaction.Source);
        Assert.AreEqual("REDACTED STORE", transaction.Merchant);
        Assert.AreEqual("Groceries", transaction.Category);
        Assert.IsNull(transaction.SourceReferenceHash);
    }

    [TestMethod]
    public void SaveCommand_CreditToggle_CreatesCreditTransaction()
    {
        var viewModel = NewValidViewModel();
        viewModel.IsCredit = true;

        viewModel.SaveCommand.Execute(null);

        Assert.IsNotNull(viewModel.LastCreatedTransaction);
        var transaction = viewModel.LastCreatedTransaction;
        Assert.AreEqual(TransactionDirection.Credit, transaction.Direction);
    }

    [TestMethod]
    public void SaveCommand_InvalidInput_DoesNotCreateTransaction()
    {
        var viewModel = NewValidViewModel();
        viewModel.Amount = "0";

        viewModel.SaveCommand.Execute(null);

        Assert.IsNull(viewModel.LastCreatedTransaction);
    }

    private static ManualTransactionEntryViewModel NewValidViewModel()
    {
        return new ManualTransactionEntryViewModel
        {
            Amount = "125.75",
            Merchant = "REDACTED STORE",
            Category = "Groceries",
            OccurredOn = new DateTime(2026, 7, 8)
        };
    }
}