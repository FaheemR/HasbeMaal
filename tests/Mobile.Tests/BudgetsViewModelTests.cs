using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class BudgetsViewModelTests
{
    [TestMethod]
    public void Constructor_HasPredictableEmptyState()
    {
        var viewModel = new BudgetsViewModel();

        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsEmpty(viewModel.Items);
    }

    [TestMethod]
    public void Load_BudgetUnderLimit_UsesBudgetEvaluationForLabels()
    {
        var viewModel = new BudgetsViewModel();
        var category = new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true);

        viewModel.Load([(category, new MoneyAmount(250m))]);

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Groceries", item.CategoryName);
        Assert.AreEqual("Essential", item.CategoryTypeText);
        Assert.AreEqual("Spent 250.00 INR", item.SpentText);
        Assert.AreEqual("Limit 1000.00 INR", item.LimitText);
        Assert.AreEqual("25% used", item.RemainingText);
        Assert.AreEqual("750.00 INR remaining", item.StatusText);
        Assert.AreEqual(0.25m, item.Progress);
    }

    [TestMethod]
    public void Load_BudgetOverLimit_UsesFactualStatus()
    {
        var viewModel = new BudgetsViewModel();
        var category = new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false);

        viewModel.Load([(category, new MoneyAmount(650m))]);

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Flexible", item.CategoryTypeText);
        Assert.AreEqual("130% used", item.RemainingText);
        Assert.AreEqual("Over limit by 150.00 INR", item.StatusText);
        Assert.AreEqual(1m, item.Progress);
    }

    [TestMethod]
    public void Load_BudgetProgressCopy_DoesNotUseAdviceWords()
    {
        var viewModel = new BudgetsViewModel();
        var category = new BudgetCategory("Education", new MoneyAmount(1000m), isEssential: true);
        string[] adviceWords = ["recommend", "should", "must", "advice", "warning", "improve"];

        viewModel.Load([(category, new MoneyAmount(1000m))]);

        var item = Assert.ContainsSingle(viewModel.Items);
        var copy = string.Join(
            " ",
            item.CategoryName,
            item.CategoryTypeText,
            item.SpentText,
            item.LimitText,
            item.RemainingText,
            item.StatusText);

        foreach (var adviceWord in adviceWords)
        {
            Assert.DoesNotContain(adviceWord, copy, StringComparison.OrdinalIgnoreCase);
        }
    }
}