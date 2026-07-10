using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class ScenarioAssumptionsTests
{
    [TestMethod]
    public void Constructor_ValidAssumptions_PreservesExplicitBucketsAndCalculatesMonthlySurplus()
    {
        var assumptions = new ScenarioAssumptions(
            "  Education goal plan  ",
            2026,
            7,
            18,
            new MoneyAmount(100000m),
            new MoneyAmount(45000m),
            new MoneyAmount(15000m),
            new MoneyAmount(5000m),
            new MoneyAmount(10000m),
            new MoneyAmount(20000m));

        Assert.AreEqual("Education goal plan", assumptions.Name);
        Assert.AreEqual(2026, assumptions.StartYear);
        Assert.AreEqual(7, assumptions.StartMonth);
        Assert.AreEqual(18, assumptions.HorizonMonths);
        Assert.AreEqual(new DateOnly(2026, 7, 1), assumptions.PeriodStart);
        Assert.AreEqual(new DateOnly(2027, 12, 1), assumptions.PeriodEnd);
        Assert.AreEqual(new MoneyAmount(100000m), assumptions.MonthlyIncome);
        Assert.AreEqual(new MoneyAmount(45000m), assumptions.EssentialMonthlyExpenses);
        Assert.AreEqual(new MoneyAmount(15000m), assumptions.DiscretionaryMonthlyExpenses);
        Assert.AreEqual(new MoneyAmount(5000m), assumptions.MonthlyDebtPayments);
        Assert.AreEqual(new MoneyAmount(10000m), assumptions.MonthlyFamilySupport);
        Assert.AreEqual(new MoneyAmount(20000m), assumptions.MonthlyGoalContributions);
        Assert.AreEqual(new MoneyAmount(95000m), assumptions.TotalMonthlyOutflow);
        Assert.AreEqual(new MoneyAmount(5000m), assumptions.MonthlySurplus);
        Assert.AreEqual(new MoneyAmount(0m), assumptions.MonthlyShortfall);
    }

    [TestMethod]
    public void Constructor_OutflowAboveIncomeCalculatesMonthlyShortfall()
    {
        var assumptions = new ScenarioAssumptions(
            "Family support scenario",
            2026,
            7,
            6,
            new MoneyAmount(50000m),
            new MoneyAmount(30000m),
            new MoneyAmount(10000m),
            new MoneyAmount(5000m),
            new MoneyAmount(12000m),
            new MoneyAmount(8000m));

        Assert.AreEqual(new MoneyAmount(65000m), assumptions.TotalMonthlyOutflow);
        Assert.AreEqual(new MoneyAmount(0m), assumptions.MonthlySurplus);
        Assert.AreEqual(new MoneyAmount(15000m), assumptions.MonthlyShortfall);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankName_Throws(string? name)
    {
        Assert.ThrowsExactly<ArgumentException>(() => NewAssumptions(name: name!));
    }

    [TestMethod]
    [DataRow(0, 7, "year")]
    [DataRow(2026, 0, "month")]
    [DataRow(2026, 13, "month")]
    public void Constructor_InvalidPeriod_Throws(int startYear, int startMonth, string expectedParamName)
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            NewAssumptions(startYear: startYear, startMonth: startMonth));

        Assert.AreEqual(expectedParamName, exception.ParamName);
    }

    [TestMethod]
    public void Constructor_InvalidHorizon_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            NewAssumptions(horizonMonths: 0));

        Assert.AreEqual("horizonMonths", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_NullMoneyAmount_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ScenarioAssumptions(
                "Scenario",
                2026,
                7,
                12,
                new MoneyAmount(100000m),
                new MoneyAmount(45000m),
                new MoneyAmount(15000m),
                new MoneyAmount(5000m),
                null!,
                new MoneyAmount(20000m)));

        Assert.AreEqual("monthlyFamilySupport", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_MixedCurrencies_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() =>
            NewAssumptions(monthlyGoalContributions: new MoneyAmount(100m, "USD")));

        Assert.AreEqual("monthlyGoalContributions", exception.ParamName);
    }

    private static ScenarioAssumptions NewAssumptions(
        string name = "Scenario",
        int startYear = 2026,
        int startMonth = 7,
        int horizonMonths = 12,
        MoneyAmount? monthlyIncome = null,
        MoneyAmount? essentialMonthlyExpenses = null,
        MoneyAmount? discretionaryMonthlyExpenses = null,
        MoneyAmount? monthlyDebtPayments = null,
        MoneyAmount? monthlyFamilySupport = null,
        MoneyAmount? monthlyGoalContributions = null) =>
        new(
            name,
            startYear,
            startMonth,
            horizonMonths,
            monthlyIncome ?? new MoneyAmount(100000m),
            essentialMonthlyExpenses ?? new MoneyAmount(45000m),
            discretionaryMonthlyExpenses ?? new MoneyAmount(15000m),
            monthlyDebtPayments ?? new MoneyAmount(5000m),
            monthlyFamilySupport ?? new MoneyAmount(10000m),
            monthlyGoalContributions ?? new MoneyAmount(20000m));
}