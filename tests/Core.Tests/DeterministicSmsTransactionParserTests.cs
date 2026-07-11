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
        Assert.AreEqual("Redacted Store", result.Merchant);
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
        Assert.AreEqual("Redacted Store", result.Merchant);
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
        Assert.AreEqual("Redacted School", result.Merchant);
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
        Assert.AreEqual("Redacted Bank", result.Merchant);
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

    [TestMethod]
    public void TryParse_CardRefund_ExtractsLeadingMerchantBodyDateAndCardTail()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "SYNTHPAY IN E COMMERC refund of Rs 1,234.00 credited to your REDACTED Bank Credit Card XX0000 on 10-JUL-26.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(1234.00m, result!.Amount.Amount);
        Assert.AreEqual(TransactionDirection.Credit, result.Direction);
        Assert.AreEqual(TransactionSource.CreditCardSms, result.Source);
        Assert.AreEqual("Synthpay", result.Merchant);
        Assert.AreEqual(new DateOnly(2026, 7, 10), result.OccurredOn);
        Assert.AreEqual("REDACTED Bank Credit Card ••0000", result.Account);
    }

    [TestMethod]
    public void TryParse_DoesNotTreatCreditedToAccountAsMerchant()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "SYNTHSTORE refund of Rs 500.00 credited to your REDACTED Debit Card ending 0000 on 05/07/2026.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual("Synthstore", result!.Merchant);
        Assert.AreEqual(new DateOnly(2026, 7, 5), result.OccurredOn);
        Assert.AreEqual("REDACTED Debit Card ••0000", result.Account);
    }

    [TestMethod]
    public void TryParse_NoBodyDate_LeavesOccurredOnNull()
    {
        var parser = new DeterministicSmsTransactionParser();

        var result = parser.TryParse("Paid Rs. 245.50 to REDACTED STORE via UPI. UPI Ref SYNTH009.");

        Assert.IsNotNull(result);
        Assert.IsNull(result!.OccurredOn);
        Assert.IsNull(result.Account);
    }
}
