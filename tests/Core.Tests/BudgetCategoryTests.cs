using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class BudgetCategoryTests
{
    [TestMethod]
    public void Constructor_TrimsNameAndPreservesValues()
    {
        var limit = new MoneyAmount(500m);

        var category = new BudgetCategory("  Groceries  ", limit, isEssential: true);

        Assert.AreEqual("Groceries", category.Name);
        Assert.AreEqual(limit, category.MonthlyLimit);
        Assert.IsTrue(category.IsEssential);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankName_Throws(string name)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BudgetCategory(name, new MoneyAmount(500m), false));
    }

    [TestMethod]
    public void Constructor_NullName_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BudgetCategory(null!, new MoneyAmount(500m), false));
    }

    [TestMethod]
    public void Constructor_NullMonthlyLimit_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BudgetCategory("Groceries", null!, false));
    }

    [TestMethod]
    public void EvaluateMonthlySpent_NullSpent_Throws()
    {
        var category = new BudgetCategory("Groceries", new MoneyAmount(500m), false);

        Assert.ThrowsExactly<ArgumentNullException>(() => category.EvaluateMonthlySpent(null!));
    }

    [TestMethod]
    public void EvaluateMonthlySpent_CurrencyMismatch_Throws()
    {
        var category = new BudgetCategory("Groceries", new MoneyAmount(500m, "INR"), false);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            category.EvaluateMonthlySpent(new MoneyAmount(100m, "USD")));
    }

    [TestMethod]
    public void EvaluateMonthlySpent_SpentBelowLimit_ReturnsRemainingAndNoOverspend()
    {
        var category = new BudgetCategory("Groceries", new MoneyAmount(500m), isEssential: true);

        var result = category.EvaluateMonthlySpent(new MoneyAmount(125m));

        Assert.AreEqual("Groceries", result.CategoryName);
        Assert.AreEqual(new MoneyAmount(500m), result.MonthlyLimit);
        Assert.AreEqual(new MoneyAmount(125m), result.Spent);
        Assert.AreEqual(new MoneyAmount(375m), result.Remaining);
        Assert.AreEqual(new MoneyAmount(0m), result.Overspend);
        Assert.AreEqual(25m, result.PercentUsed);
        Assert.IsTrue(result.IsEssential);
    }

    [TestMethod]
    public void EvaluateMonthlySpent_SpentEqualsLimit_ReturnsNoRemainingOrOverspend()
    {
        var category = new BudgetCategory("Education", new MoneyAmount(500m), isEssential: false);

        var result = category.EvaluateMonthlySpent(new MoneyAmount(500m));

        Assert.AreEqual(new MoneyAmount(0m), result.Remaining);
        Assert.AreEqual(new MoneyAmount(0m), result.Overspend);
        Assert.AreEqual(100m, result.PercentUsed);
        Assert.IsFalse(result.IsEssential);
    }

    [TestMethod]
    public void EvaluateMonthlySpent_SpentAboveLimit_ReturnsOverspendAndNoRemaining()
    {
        var category = new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false);

        var result = category.EvaluateMonthlySpent(new MoneyAmount(650m));

        Assert.AreEqual(new MoneyAmount(0m), result.Remaining);
        Assert.AreEqual(new MoneyAmount(150m), result.Overspend);
        Assert.AreEqual(130m, result.PercentUsed);
        Assert.IsFalse(result.IsEssential);
    }

    [TestMethod]
    public void EvaluateMonthlySpent_ZeroLimitAndZeroSpent_ReturnsZeroPercent()
    {
        var category = new BudgetCategory("Savings", new MoneyAmount(0m), isEssential: false);

        var result = category.EvaluateMonthlySpent(new MoneyAmount(0m));

        Assert.AreEqual(new MoneyAmount(0m), result.Remaining);
        Assert.AreEqual(new MoneyAmount(0m), result.Overspend);
        Assert.AreEqual(0m, result.PercentUsed);
    }

    [TestMethod]
    public void EvaluateMonthlySpent_ZeroLimitAndPositiveSpent_ReturnsHundredPercentAndOverspend()
    {
        var category = new BudgetCategory("Subscriptions", new MoneyAmount(0m), isEssential: false);

        var result = category.EvaluateMonthlySpent(new MoneyAmount(25m));

        Assert.AreEqual(new MoneyAmount(0m), result.Remaining);
        Assert.AreEqual(new MoneyAmount(25m), result.Overspend);
        Assert.AreEqual(100m, result.PercentUsed);
    }
}