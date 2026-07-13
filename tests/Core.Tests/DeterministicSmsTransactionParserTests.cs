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

    [TestMethod]
    public void TryParse_AccountStatementDebitWithNoPayee_MerchantIsUnknown()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message = "Your A/c XXXXXXXX0000 has been debited by Rs 1,500.00 on 09-JUL-26.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual("Unknown", result!.Merchant);
        Assert.AreEqual(ParseConfidence.Medium, result.Confidence);
        Assert.AreEqual(TransactionDirection.Debit, result.Direction);
        Assert.AreEqual("A/c ••0000", result.Account);
    }

    [TestMethod]
    public void TryParse_LeadingBareNumberFragment_IsNotUsedAsMerchant()
    {
        var parser = new DeterministicSmsTransactionParser();

        var result = parser.TryParse("11 Rs 3,000.00 credited.");

        Assert.IsNotNull(result);
        Assert.AreEqual("Unknown", result!.Merchant);
        Assert.AreEqual(TransactionDirection.Credit, result.Direction);
    }

    // Synthetic fixtures modeled on real HDFC/JK Bank formats (redacted, no real data).

    [TestMethod]
    public void TryParse_HdfcUpiSend_ExtractsPayeeAfterTo()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Sent Rs 500.00 From REDACTED SELF A/C x0000 To REDACTED PAYEE On 09-JUL-26 Ref 123456789012 Not You? Call 18000.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(500.00m, result!.Amount.Amount);
        Assert.AreEqual(TransactionDirection.Debit, result.Direction);
        Assert.AreEqual("Redacted Payee", result.Merchant);
        Assert.AreEqual(new DateOnly(2026, 7, 9), result.OccurredOn);
    }

    [TestMethod]
    public void TryParse_HdfcCardSpend_ExtractsMerchantAfterAt()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Spent Rs 250.00 On HDFC Bank Card x0000 At REDACTED STORE On 09-JUL-26. Not You? Call 18000.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Debit, result!.Direction);
        Assert.AreEqual("Redacted Store", result.Merchant);
    }

    [TestMethod]
    public void TryParse_JkCredit_ExtractsPayerAfterFrom()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Rs 300.00 credited to REDACTED SELF A/c no. XX0000 via UPI from REDACTED PAYER on 09-JUL-26. (UPI Ref No:123456789012).";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Credit, result!.Direction);
        Assert.AreEqual("Redacted Payer", result.Merchant);
        Assert.AreEqual("123456789012", result.Reference);
    }

    [TestMethod]
    public void TryParse_UpiAutoPayTowardsVpa_ExtractsVpaAsMerchant()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Your account has been successfully debited with Rs 200.00 on 09-JUL-26 towards synthmerchant@okhdfcbank UPI AutoPay.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Debit, result!.Direction);
        Assert.AreEqual("synthmerchant@okhdfcbank", result.Merchant);
    }

    [TestMethod]
    public void TryParse_HdfcCardTxnAtVpa_TreatedAsDebitWithVpaMerchant()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Txn Rs 100.00 On HDFC Bank Card 0000 At synthmerchant@okaxis by UPI 123456789012 On 09-07. Not You? Call 18000.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Debit, result!.Direction);
        Assert.AreEqual("synthmerchant@okaxis", result.Merchant);
    }

    [TestMethod]
    public void TryParse_JkOnAccountOfUpiBankCode_HasNoPayeeSoUnknown()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Your A/C XX0000 is Debited by Rs 500.00 at 14:30 on account of UPI/HDFC/1234567. A/C Bal is Rs 1000.00 Cr, Available Bal is Rs 900.00 Cr JK BANK";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Debit, result!.Direction);
        Assert.AreEqual("Unknown", result.Merchant);
    }

    [TestMethod]
    public void TryParse_BalanceAlert_IsIgnored()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "Available Bal in HDFC Bank A/c XX0000 as on 09-JUL-26 is Rs 5000.00. For updated A/C Bal dial 18000.";

        var result = parser.TryParse(message);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryParse_CardmemberPaymentBoilerplate_ResolvesToUnknownMerchant()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "HDFC Bank Cardmember, Payment of Rs 16365.00 was credited to your card ending 8728 on 03-JUL-26.";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Credit, result!.Direction);
        Assert.AreEqual("Unknown", result.Merchant);
    }

    [TestMethod]
    public void TryParse_DearCardmemberBoilerplate_ResolvesToUnknownMerchant()
    {
        var parser = new DeterministicSmsTransactionParser();
        const string message =
            "DEAR HDFCBANK CARDMEMBER, PAYMENT OF Rs 16365.00 RECEIVED TOWARDS YOUR CREDIT CARD ENDING WITH 8728 ON 03-JUL-26. YOUR AVAILABLE LIMIT IS Rs 50000.00";

        var result = parser.TryParse(message);

        Assert.IsNotNull(result);
        Assert.AreEqual(TransactionDirection.Credit, result!.Direction);
        Assert.AreEqual("Unknown", result.Merchant);
    }
}
