using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class TransactionDuplicateDetectorTests
{
    [TestMethod]
    public void HasDuplicate_SameSourceReferenceHashAndDifferentIds_ReturnsTrue()
    {
        var existing = NewTransaction(Guid.NewGuid(), "HASH001");
        var candidate = NewTransaction(Guid.NewGuid(), "hash001");

        var result = TransactionDuplicateDetector.HasDuplicate(candidate, [existing]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasDuplicate_SameSourceReferenceHashAndSameId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var existing = NewTransaction(id, "HASH002");
        var candidate = NewTransaction(id, "HASH002");

        var result = TransactionDuplicateDetector.HasDuplicate(candidate, [existing]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasDuplicate_DifferentSourceReferenceHash_ReturnsFalse()
    {
        var existing = NewTransaction(Guid.NewGuid(), "HASH003");
        var candidate = NewTransaction(Guid.NewGuid(), "HASH004");

        var result = TransactionDuplicateDetector.HasDuplicate(candidate, [existing]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasDuplicate_MissingCandidateSourceReferenceHash_ReturnsFalse()
    {
        var existing = NewTransaction(Guid.NewGuid(), "HASH005");
        var candidate = NewTransaction(Guid.NewGuid(), null);

        var result = TransactionDuplicateDetector.HasDuplicate(candidate, [existing]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasDuplicate_MissingExistingSourceReferenceHash_ReturnsFalse()
    {
        var existing = NewTransaction(Guid.NewGuid(), null);
        var candidate = NewTransaction(Guid.NewGuid(), "HASH006");

        var result = TransactionDuplicateDetector.HasDuplicate(candidate, [existing]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasDuplicate_NullCandidate_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            TransactionDuplicateDetector.HasDuplicate(null!, []));
    }

    [TestMethod]
    public void HasDuplicate_NullExistingTransactions_Throws()
    {
        var candidate = NewTransaction(Guid.NewGuid(), "HASH007");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            TransactionDuplicateDetector.HasDuplicate(candidate, null!));
    }

    private static FinancialTransaction NewTransaction(Guid id, string? sourceReferenceHash) => new(
        id,
        new MoneyAmount(100m),
        TransactionDirection.Debit,
        TransactionSource.UpiSms,
        new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
        "REDACTED STORE",
        "Groceries",
        sourceReferenceHash);
}