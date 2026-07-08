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