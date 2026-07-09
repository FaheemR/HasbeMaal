using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class DeterministicSmsTransactionParserTests
{
    [TestMethod]
    public void TryParse_UpiDebitWithReferenceAndToMerchant_ReturnsDebitTransaction()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "Paid Rs. 245.50 to REDACTED STORE via UPI. UPI Ref SYNTH001.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(245.50m, result!.Amount.Amount);
        Assert.AreEqual("INR", result.Amount.Currency);
        Assert.AreEqual("REDACTED STORE", result.Merchant);
        Assert.AreEqual(TransactionDirection.Debit, result.Direction);
        Assert.AreEqual(TransactionSource.UpiSms, result.Source);
        Assert.AreEqual("SYNTH001", result.Reference);
        Assert.AreEqual(ParseConfidence.High, result.Confidence);
    }

    [TestMethod]
    public void TryParse_UpiCreditWithFromMerchant_ReturnsCreditTransaction()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "Received INR 850.00 from REDACTED STORE via UPI. UPI Ref SYNTH002.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(850.00m, result!.Amount.Amount);
        Assert.AreEqual("INR", result.Amount.Currency);
        Assert.AreEqual("REDACTED STORE", result.Merchant);
        Assert.AreEqual(TransactionDirection.Credit, result.Direction);
        Assert.AreEqual(TransactionSource.UpiSms, result.Source);
        Assert.AreEqual("SYNTH002", result.Reference);
        Assert.AreEqual(ParseConfidence.High, result.Confidence);
    }

    [TestMethod]
    public void TryParse_BankDebitWithAccountAndAtMerchant_ReturnsBankDebitTransaction()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "A/c XX0000 debited by INR 1,200.00 at REDACTED SCHOOL on 08-Jul. Ref SYNTH003.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(1200.00m, result!.Amount.Amount);
        Assert.AreEqual("INR", result.Amount.Currency);
        Assert.AreEqual("REDACTED SCHOOL", result.Merchant);
        Assert.AreEqual(TransactionDirection.Debit, result.Direction);
        Assert.AreEqual(TransactionSource.BankSms, result.Source);
        Assert.AreEqual("SYNTH003", result.Reference);
        Assert.AreEqual(ParseConfidence.High, result.Confidence);
    }

    [TestMethod]
    public void TryParse_BankSalaryCreditWithReference_ReturnsBankCreditTransaction()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "Salary of INR 50,000.00 credited from REDACTED BANK. Ref SYNTH004.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(50000.00m, result!.Amount.Amount);
        Assert.AreEqual("INR", result.Amount.Currency);
        Assert.AreEqual("REDACTED BANK", result.Merchant);
        Assert.AreEqual(TransactionDirection.Credit, result.Direction);
        Assert.AreEqual(TransactionSource.BankSms, result.Source);
        Assert.AreEqual("SYNTH004", result.Reference);
        Assert.AreEqual(ParseConfidence.High, result.Confidence);
    }

    [TestMethod]
    public void TryParse_NonFinancialSms_ReturnsNull()
    {
        var parser = new DeterministicSmsTransactionParser();

        var result = parser.TryParse("Your appointment is confirmed for tomorrow.");

        Assert.IsNull(result);
    }
}
