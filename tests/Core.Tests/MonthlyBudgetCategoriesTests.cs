using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class MonthlyBudgetCategoriesTests
{
    [TestMethod]
    public void Constructor_ValidCategories_PreservesPeriodAndOrdersByName()
    {
        var categories = new[]
        {
            new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false),
            new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true),
        };

        var monthlyBudget = new MonthlyBudgetCategories(2026, 7, categories);

        Assert.AreEqual(2026, monthlyBudget.Year);
        Assert.AreEqual(7, monthlyBudget.Month);
        CollectionAssert.AreEqual(
            new[] { "Groceries", "Transport" },
            monthlyBudget.Categories.Select(category => category.Name).ToArray());
    }

    [TestMethod]
    public void Constructor_CopiesCategoryList()
    {
        var categories = new List<BudgetCategory>
        {
            new("Groceries", new MoneyAmount(1000m), isEssential: true),
        };

        var monthlyBudget = new MonthlyBudgetCategories(2026, 7, categories);
        categories.Add(new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false));

        var category = Assert.ContainsSingle(monthlyBudget.Categories);
        Assert.AreEqual("Groceries", category.Name);
    }

    [TestMethod]
    public void Empty_ValidPeriod_ReturnsNoCategories()
    {
        var monthlyBudget = MonthlyBudgetCategories.Empty(2026, 7);

        Assert.AreEqual(2026, monthlyBudget.Year);
        Assert.AreEqual(7, monthlyBudget.Month);
        Assert.IsEmpty(monthlyBudget.Categories);
    }

    [TestMethod]
    public void Constructor_NullCategories_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new MonthlyBudgetCategories(2026, 7, null!));
    }

    [TestMethod]
    public void Constructor_NullCategory_Throws()
    {
        BudgetCategory?[] categories =
        [
            new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true),
            null,
        ];

        var exception = Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthlyBudgetCategories(2026, 7, categories!));

        Assert.AreEqual("categories", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_DuplicateNamesIgnoringCase_Throws()
    {
        var categories = new[]
        {
            new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true),
            new BudgetCategory(" groceries ", new MoneyAmount(1200m), isEssential: true),
        };

        var exception = Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthlyBudgetCategories(2026, 7, categories));

        Assert.AreEqual("categories", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_MixedCurrencies_Throws()
    {
        var categories = new[]
        {
            new BudgetCategory("Groceries", new MoneyAmount(1000m, "INR"), isEssential: true),
            new BudgetCategory("Travel", new MoneyAmount(200m, "USD"), isEssential: false),
        };

        var exception = Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthlyBudgetCategories(2026, 7, categories));

        Assert.AreEqual("categories", exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, 7, "year")]
    [DataRow(2026, 0, "month")]
    [DataRow(2026, 13, "month")]
    public void Constructor_InvalidPeriod_Throws(int year, int month, string expectedParamName)
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new MonthlyBudgetCategories(year, month, []));

        Assert.AreEqual(expectedParamName, exception.ParamName);
    }
}