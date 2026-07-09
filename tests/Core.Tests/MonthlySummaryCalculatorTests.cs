using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class MonthlySummaryCalculatorTests
{
    [TestMethod]
    public void Calculate_MonthlyTransactions_ReturnsIncomeExpensesSurplusAndCategories()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED SALARY", "Income", 1000m, TransactionDirection.Credit, 2026, 7, 1),
            NewTransaction("REDACTED STORE", "Groceries", 125.75m, TransactionDirection.Debit, 2026, 7, 8),
            NewTransaction("REDACTED MARKET", "Groceries", 80.25m, TransactionDirection.Debit, 2026, 7, 10),
            NewTransaction("REDACTED SCHOOL", "Education", 450m, TransactionDirection.Debit, 2026, 7, 12),
            NewTransaction("REDACTED CLINIC", "Health", 300m, TransactionDirection.Debit, 2026, 6, 30)
        };

        var summary = MonthlySummaryCalculator.Calculate(transactions, 2026, 7);

        Assert.AreEqual(2026, summary.Year);
        Assert.AreEqual(7, summary.Month);
        Assert.AreEqual(1000m, summary.Income.Amount);
        Assert.AreEqual(656m, summary.Expenses.Amount);
        Assert.AreEqual(344m, summary.Surplus.Amount);
        Assert.AreEqual(0m, summary.Overspend.Amount);
        Assert.HasCount(2, summary.Categories);
        Assert.AreEqual("Education", summary.Categories[0].Category);
        Assert.AreEqual(450m, summary.Categories[0].Expenses.Amount);
        Assert.AreEqual("Groceries", summary.Categories[1].Category);
        Assert.AreEqual(206m, summary.Categories[1].Expenses.Amount);
    }

    [TestMethod]
    public void Calculate_NoTransactionsForMonth_ReturnsZeroInrSummary()
    {
        var summary = MonthlySummaryCalculator.Calculate([], 2026, 7);

        Assert.AreEqual(0m, summary.Income.Amount);
        Assert.AreEqual(0m, summary.Expenses.Amount);
        Assert.AreEqual(0m, summary.Surplus.Amount);
        Assert.AreEqual(0m, summary.Overspend.Amount);
        Assert.AreEqual("INR", summary.Income.Currency);
        Assert.IsEmpty(summary.Categories);
    }

    [TestMethod]
    public void Calculate_ExpenseGreaterThanIncome_ReportsOverspend()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED STORE", "Groceries", 125m, TransactionDirection.Debit, 2026, 7, 8)
        };

        var summary = MonthlySummaryCalculator.Calculate(transactions, 2026, 7);

        Assert.AreEqual(0m, summary.Surplus.Amount);
        Assert.AreEqual(125m, summary.Overspend.Amount);
        Assert.AreEqual(125m, summary.Expenses.Amount);
    }

    [TestMethod]
    public void Calculate_IncomeEqualsExpenses_ReturnsNoSurplusOrOverspend()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED SALARY", "Income", 125m, TransactionDirection.Credit, 2026, 7, 1),
            NewTransaction("REDACTED STORE", "Groceries", 125m, TransactionDirection.Debit, 2026, 7, 8)
        };

        var summary = MonthlySummaryCalculator.Calculate(transactions, 2026, 7);

        Assert.AreEqual(0m, summary.Surplus.Amount);
        Assert.AreEqual(0m, summary.Overspend.Amount);
    }

    [TestMethod]
    public void Calculate_MixedCurrencies_Throws()
    {
        var transactions = new[]
        {
            NewTransaction("REDACTED STORE", "Groceries", 125m, TransactionDirection.Debit, 2026, 7, 8, "INR"),
            NewTransaction("REDACTED STORE", "Groceries", 10m, TransactionDirection.Debit, 2026, 7, 9, "USD")
        };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            MonthlySummaryCalculator.Calculate(transactions, 2026, 7));
    }

    [TestMethod]
    public void Calculate_NullTransactions_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => MonthlySummaryCalculator.Calculate(null!, 2026, 7));
    }

    [TestMethod]
    [DataRow(0, 7)]
    [DataRow(2026, 0)]
    [DataRow(2026, 13)]
    public void Calculate_InvalidYearOrMonth_Throws(int year, int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MonthlySummaryCalculator.Calculate([], year, month));
    }

    private static FinancialTransaction NewTransaction(
        string merchant,
        string category,
        decimal amount,
        TransactionDirection direction,
        int year,
        int month,
        int day,
        string currency = "INR")
    {
        return new FinancialTransaction(
            Guid.NewGuid(),
            new MoneyAmount(amount, currency),
            direction,
            TransactionSource.ManualCash,
            new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero),
            merchant,
            category,
            sourceReferenceHash: null);
    }
}