using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class RecurringTransactionDetectorTests
{
    [TestMethod]
    public void DetectMonthlyPatterns_ConsecutiveMatchingTransactions_ReturnsMonthlyPattern()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 1, 15),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 2, 16),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 3, 14),
            NewTransaction("REDACTED STORE", "Groceries", 499m, 2026, 1, 15),
        };

        var pattern = Assert.ContainsSingle(RecurringTransactionDetector.DetectMonthlyPatterns(transactions));

        Assert.AreEqual("REDACTED STREAM", pattern.Merchant);
        Assert.AreEqual("Subscriptions", pattern.Category);
        Assert.AreEqual(TransactionDirection.Debit, pattern.Direction);
        Assert.AreEqual(new MoneyAmount(499m), pattern.Amount);
        Assert.AreEqual(new DateOnly(2026, 1, 15), pattern.FirstOccurrence);
        Assert.AreEqual(new DateOnly(2026, 3, 14), pattern.LastOccurrence);
        Assert.AreEqual(3, pattern.OccurrenceCount);
        Assert.AreEqual(15, pattern.ExpectedDayOfMonth);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_GroupsBySanitizedMetadataIgnoringCase()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 1, 15),
            NewTransaction("redacted stream", "subscriptions", 499m, 2026, 2, 15),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 3, 15),
        };

        var pattern = Assert.ContainsSingle(RecurringTransactionDetector.DetectMonthlyPatterns(transactions));

        Assert.AreEqual("REDACTED STREAM", pattern.Merchant);
        Assert.AreEqual("Subscriptions", pattern.Category);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_DifferentAmountDoesNotCreatePattern()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 1, 15),
            NewTransaction("REDACTED STREAM", "Subscriptions", 599m, 2026, 2, 15),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 3, 15),
        };

        var patterns = RecurringTransactionDetector.DetectMonthlyPatterns(transactions);

        Assert.IsEmpty(patterns);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_NonConsecutiveMonthsDoesNotCreatePattern()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED GYM", "Health", 1200m, 2026, 1, 5),
            NewTransaction("REDACTED GYM", "Health", 1200m, 2026, 3, 5),
            NewTransaction("REDACTED GYM", "Health", 1200m, 2026, 4, 5),
        };

        var patterns = RecurringTransactionDetector.DetectMonthlyPatterns(transactions);

        Assert.IsEmpty(patterns);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_DayOutsideToleranceDoesNotCreatePattern()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED GYM", "Health", 1200m, 2026, 1, 5),
            NewTransaction("REDACTED GYM", "Health", 1200m, 2026, 2, 12),
            NewTransaction("REDACTED GYM", "Health", 1200m, 2026, 3, 5),
        };

        var patterns = RecurringTransactionDetector.DetectMonthlyPatterns(transactions);

        Assert.IsEmpty(patterns);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_SameMonthDuplicatesCountAsSingleOccurrence()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 1, 15),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 1, 16),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 2, 15),
            NewTransaction("REDACTED STREAM", "Subscriptions", 499m, 2026, 3, 15),
        };

        var pattern = Assert.ContainsSingle(RecurringTransactionDetector.DetectMonthlyPatterns(transactions));

        Assert.AreEqual(3, pattern.OccurrenceCount);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_MinimumOccurrencesCanBeTwo()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED SCHOOL", "Education", 2000m, 2026, 1, 10),
            NewTransaction("REDACTED SCHOOL", "Education", 2000m, 2026, 2, 10),
        };

        var pattern = Assert.ContainsSingle(RecurringTransactionDetector.DetectMonthlyPatterns(
            transactions,
            minimumOccurrences: 2));

        Assert.AreEqual("Education", pattern.Category);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_OrdersPatternsDeterministically()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED ZED", "Subscriptions", 100m, 2026, 1, 5),
            NewTransaction("REDACTED ZED", "Subscriptions", 100m, 2026, 2, 5),
            NewTransaction("REDACTED ZED", "Subscriptions", 100m, 2026, 3, 5),
            NewTransaction("REDACTED APP", "Subscriptions", 100m, 2026, 1, 5),
            NewTransaction("REDACTED APP", "Subscriptions", 100m, 2026, 2, 5),
            NewTransaction("REDACTED APP", "Subscriptions", 100m, 2026, 3, 5),
        };

        var patterns = RecurringTransactionDetector.DetectMonthlyPatterns(transactions);

        Assert.HasCount(2, patterns);
        Assert.AreEqual("REDACTED APP", patterns[0].Merchant);
        Assert.AreEqual("REDACTED ZED", patterns[1].Merchant);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_NullTransactions_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            RecurringTransactionDetector.DetectMonthlyPatterns(null!));
    }

    [TestMethod]
    [DataRow(0, "minimumOccurrences")]
    [DataRow(1, "minimumOccurrences")]
    public void DetectMonthlyPatterns_InvalidMinimumOccurrences_Throws(
        int minimumOccurrences,
        string expectedParamName)
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            RecurringTransactionDetector.DetectMonthlyPatterns([], minimumOccurrences));

        Assert.AreEqual(expectedParamName, exception.ParamName);
    }

    [TestMethod]
    public void DetectMonthlyPatterns_NegativeDayTolerance_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            RecurringTransactionDetector.DetectMonthlyPatterns([], dayTolerance: -1));

        Assert.AreEqual("dayTolerance", exception.ParamName);
    }

    private static FinancialTransaction NewTransaction(
        string merchant,
        string category,
        decimal amount,
        int year,
        int month,
        int day,
        TransactionDirection direction = TransactionDirection.Debit,
        string currency = "INR") =>
        new(
            Guid.NewGuid(),
            new MoneyAmount(amount, currency),
            direction,
            TransactionSource.ManualCash,
            new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero),
            merchant,
            category,
            sourceReferenceHash: null);
}