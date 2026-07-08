using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class DeterministicSmsTransactionParserTests
{
    [TestMethod]
    public void TryParse_GPayDebitSms_ReturnsDebitTransaction()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "Paid Rs. 245.50 to REDACTED STORE via GPay on 08 Jul. UPI ref no SYNTH001.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(245.50m, result!.Amount.Amount);
        Assert.AreEqual("INR", result.Amount.Currency);
        Assert.AreEqual("REDACTED STORE", result.Merchant);
        Assert.AreEqual(TransactionDirection.Debit, result.Direction);
        Assert.AreEqual(TransactionSource.UpiSms, result.Source);
        Assert.AreEqual(ParseConfidence.High, result.Confidence);
    }

    [TestMethod]
    public void TryParse_BankDebitSms_ReturnsBankSource()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "A/c XX0000 debited by INR 1,200.00 at REDACTED SCHOOL on 08-Jul. Ref SYNTH002.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(1200.00m, result!.Amount.Amount);
        Assert.AreEqual("REDACTED SCHOOL", result.Merchant);
        Assert.AreEqual(TransactionDirection.Debit, result.Direction);
        Assert.AreEqual(TransactionSource.BankSms, result.Source);
    }

    [TestMethod]
    public void TryParse_NonFinancialSms_ReturnsNull()
    {
        var parser = new DeterministicSmsTransactionParser();

        var result = parser.TryParse("Your appointment is confirmed for tomorrow.");

        Assert.IsNull(result);
    }
}
