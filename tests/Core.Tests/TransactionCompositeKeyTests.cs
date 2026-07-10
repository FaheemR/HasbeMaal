using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class TransactionCompositeKeyTests
{
    [TestMethod]
    public void FromTransaction_TruncatesOccurrenceToMinuteInUtc()
    {
        var withSeconds = NewTransaction(
            amount: 100m,
            merchant: "REDACTED STORE",
            occurredAt: new DateTimeOffset(2026, 7, 9, 10, 30, 45, 500, TimeSpan.Zero));
        var withoutSeconds = NewTransaction(
            amount: 100m,
            merchant: "REDACTED STORE",
            occurredAt: new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero));

        var withSecondsKey = TransactionCompositeKey.FromTransaction(withSeconds);
        var withoutSecondsKey = TransactionCompositeKey.FromTransaction(withoutSeconds);

        Assert.AreEqual(withoutSecondsKey, withSecondsKey);
        Assert.AreEqual(TimeSpan.Zero, withSecondsKey.OccurredAtMinute.Offset);
    }

    [TestMethod]
    public void FromTransaction_EqualInstantsWithDifferentOffsets_ProduceEqualKeys()
    {
        var utc = NewTransaction(
            amount: 100m,
            merchant: "REDACTED STORE",
            occurredAt: new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero));
        var offset = NewTransaction(
            amount: 100m,
            merchant: "REDACTED STORE",
            occurredAt: new DateTimeOffset(2026, 7, 9, 16, 0, 0, TimeSpan.FromHours(5.5)));

        Assert.AreEqual(
            TransactionCompositeKey.FromTransaction(utc),
            TransactionCompositeKey.FromTransaction(offset));
    }

    [TestMethod]
    public void FromTransaction_NormalizesMerchantCaseInsensitively()
    {
        var lower = NewTransaction(amount: 100m, merchant: "redacted store");
        var upper = NewTransaction(amount: 100m, merchant: "  REDACTED STORE  ");

        Assert.AreEqual(
            TransactionCompositeKey.FromTransaction(lower),
            TransactionCompositeKey.FromTransaction(upper));
    }

    [TestMethod]
    public void FromTransaction_DifferentMinute_ProducesDifferentKeys()
    {
        var first = NewTransaction(
            amount: 100m,
            merchant: "REDACTED STORE",
            occurredAt: new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero));
        var second = NewTransaction(
            amount: 100m,
            merchant: "REDACTED STORE",
            occurredAt: new DateTimeOffset(2026, 7, 9, 10, 31, 0, TimeSpan.Zero));

        Assert.AreNotEqual(
            TransactionCompositeKey.FromTransaction(first),
            TransactionCompositeKey.FromTransaction(second));
    }

    [TestMethod]
    public void FromTransaction_DifferentAmount_ProducesDifferentKeys()
    {
        var first = NewTransaction(amount: 100m, merchant: "REDACTED STORE");
        var second = NewTransaction(amount: 101m, merchant: "REDACTED STORE");

        Assert.AreNotEqual(
            TransactionCompositeKey.FromTransaction(first),
            TransactionCompositeKey.FromTransaction(second));
    }

    [TestMethod]
    public void FromTransaction_NullTransaction_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => TransactionCompositeKey.FromTransaction(null!));
    }

    private static FinancialTransaction NewTransaction(
        decimal amount,
        string merchant,
        DateTimeOffset? occurredAt = null) => new(
        Guid.NewGuid(),
        new MoneyAmount(amount),
        TransactionDirection.Debit,
        TransactionSource.UpiSms,
        occurredAt ?? new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero),
        merchant,
        "Uncategorized",
        sourceReferenceHash: null);
}
