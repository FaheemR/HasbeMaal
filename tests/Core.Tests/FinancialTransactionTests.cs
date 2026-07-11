using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class FinancialTransactionTests
{
    [TestMethod]
    public void Constructor_ValidTransaction_SetsProperties()
    {
        var id = Guid.NewGuid();
        var amount = new MoneyAmount(125.75m);
        var occurredAt = new DateTimeOffset(2026, 7, 8, 10, 15, 0, TimeSpan.Zero);

        var transaction = new FinancialTransaction(
            id,
            amount,
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            occurredAt,
            " REDACTED STORE ",
            " Groceries ",
            " abc123 ");

        Assert.AreEqual(id, transaction.Id);
        Assert.AreSame(amount, transaction.Amount);
        Assert.AreEqual(TransactionDirection.Debit, transaction.Direction);
        Assert.AreEqual(TransactionSource.ManualCash, transaction.Source);
        Assert.AreEqual(occurredAt, transaction.OccurredAt);
        Assert.AreEqual("REDACTED STORE", transaction.Merchant);
        Assert.AreEqual("Groceries", transaction.Category);
        Assert.AreEqual("ABC123", transaction.SourceReferenceHash);
    }

    [TestMethod]
    public void Constructor_RawSourceReference_IsTrimmedAndRetained()
    {
        var transaction = NewTransaction(sourceReference: "  SYNTH-UPI-REF-001  ");

        Assert.AreEqual("SYNTH-UPI-REF-001", transaction.SourceReference);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankSourceReference_BecomesNull(string? sourceReference)
    {
        var transaction = NewTransaction(sourceReference: sourceReference);

        Assert.IsNull(transaction.SourceReference);
    }

    [TestMethod]
    public void ToString_ExcludesRawReferenceAndHash()
    {
        var transaction = new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(125.75m),
            TransactionDirection.Debit,
            TransactionSource.UpiSms,
            new DateTimeOffset(2026, 7, 8, 10, 15, 0, TimeSpan.Zero),
            "REDACTED STORE",
            "Groceries",
            sourceReferenceHash: "ABC123HASH",
            sourceReference: "SYNTH-UPI-REF-002",
            account: "REDACTED Bank Credit Card ••0000",
            sourceMessage: "SYNTH ORIGINAL SMS BODY XX0000");

        var text = transaction.ToString();

        Assert.DoesNotContain("SYNTH-UPI-REF-002", text);
        Assert.DoesNotContain("ABC123HASH", text);
        Assert.DoesNotContain("SourceReference", text);
        Assert.DoesNotContain("••0000", text);
        Assert.DoesNotContain("SYNTH ORIGINAL SMS BODY", text);
        Assert.DoesNotContain("Account", text);
        Assert.DoesNotContain("SourceMessage", text);
    }

    [TestMethod]
    public void Constructor_AccountAndSourceMessage_AreTrimmedAndRetained()
    {
        var transaction = new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(125.75m),
            TransactionDirection.Debit,
            TransactionSource.UpiSms,
            new DateTimeOffset(2026, 7, 8, 10, 15, 0, TimeSpan.Zero),
            "REDACTED STORE",
            "Groceries",
            sourceReferenceHash: null,
            sourceReference: null,
            account: "  REDACTED Bank Credit Card ••0000  ",
            sourceMessage: "  SYNTH ORIGINAL SMS  ");

        Assert.AreEqual("REDACTED Bank Credit Card ••0000", transaction.Account);
        Assert.AreEqual("SYNTH ORIGINAL SMS", transaction.SourceMessage);
    }

    private static FinancialTransaction NewTransaction(string? sourceReference) => new(
        Guid.NewGuid(),
        new MoneyAmount(125.75m),
        TransactionDirection.Debit,
        TransactionSource.UpiSms,
        new DateTimeOffset(2026, 7, 8, 10, 15, 0, TimeSpan.Zero),
        "REDACTED STORE",
        "Groceries",
        sourceReferenceHash: null,
        sourceReference: sourceReference);

    [TestMethod]
    public void Constructor_EmptyId_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new FinancialTransaction(
            Guid.Empty,
            new MoneyAmount(10m),
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            DateTimeOffset.UtcNow,
            "REDACTED STORE",
            "Groceries",
            null));
    }

    [TestMethod]
    public void Constructor_NullAmount_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new FinancialTransaction(
            Guid.NewGuid(),
            null!,
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            DateTimeOffset.UtcNow,
            "REDACTED STORE",
            "Groceries",
            null));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankMerchant_Throws(string merchant)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(10m),
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            DateTimeOffset.UtcNow,
            merchant,
            "Groceries",
            null));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankCategory_Throws(string category)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(10m),
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            DateTimeOffset.UtcNow,
            "REDACTED STORE",
            category,
            null));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_MissingSourceReferenceHash_StoresNull(string? sourceReferenceHash)
    {
        var transaction = new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(10m),
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            DateTimeOffset.UtcNow,
            "REDACTED STORE",
            "Groceries",
            sourceReferenceHash);

        Assert.IsNull(transaction.SourceReferenceHash);
    }
}