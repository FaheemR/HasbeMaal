using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class FinancialGoalTests
{
    [TestMethod]
    public void Constructor_ValidGoal_TrimsTextAndPreservesValues()
    {
        var goal = new FinancialGoal(
            "  Emergency fund  ",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            new DateOnly(2027, 6, 30),
            "  Household reserve  ");

        Assert.AreEqual("Emergency fund", goal.Name);
        Assert.AreEqual(new MoneyAmount(150000m), goal.TargetAmount);
        Assert.AreEqual(new MoneyAmount(25000m), goal.CurrentAmount);
        Assert.AreEqual(new DateOnly(2027, 6, 30), goal.TargetDate);
        Assert.AreEqual("Household reserve", goal.Purpose);
    }

    [TestMethod]
    public void CreateMonthlyContributionProjection_RemainingGoalCalculatesInclusiveMonthlyContribution()
    {
        var goal = new FinancialGoal(
            "Education goal",
            new MoneyAmount(120000m),
            new MoneyAmount(30000m),
            new DateOnly(2026, 12, 31),
            "School fees");

        var projection = goal.CreateMonthlyContributionProjection(new DateOnly(2026, 7, 10));

        Assert.AreEqual("Education goal", projection.GoalName);
        Assert.AreEqual(new DateOnly(2026, 7, 1), projection.ProjectionStartMonth);
        Assert.AreEqual(new DateOnly(2026, 12, 1), projection.TargetMonth);
        Assert.AreEqual(6, projection.MonthsRemaining);
        Assert.AreEqual(new MoneyAmount(90000m), projection.RemainingAmount);
        Assert.AreEqual(new MoneyAmount(15000m), projection.RequiredMonthlyContribution);
        Assert.IsFalse(projection.IsTargetReached);
    }

    [TestMethod]
    public void CreateMonthlyContributionProjection_TargetReachedReturnsZeroRequiredContribution()
    {
        var goal = new FinancialGoal(
            "Completed goal",
            new MoneyAmount(50000m),
            new MoneyAmount(75000m),
            new DateOnly(2026, 7, 31),
            "Already funded");

        var projection = goal.CreateMonthlyContributionProjection(new DateOnly(2026, 8, 1));

        Assert.AreEqual(0, projection.MonthsRemaining);
        Assert.AreEqual(new MoneyAmount(0m), projection.RemainingAmount);
        Assert.AreEqual(new MoneyAmount(0m), projection.RequiredMonthlyContribution);
        Assert.IsTrue(projection.IsTargetReached);
    }

    [TestMethod]
    public void CreateMonthlyContributionProjection_StartAfterTargetMonthWithRemainingAmount_Throws()
    {
        var goal = NewGoal(targetDate: new DateOnly(2026, 7, 31));

        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            goal.CreateMonthlyContributionProjection(new DateOnly(2026, 8, 1)));

        Assert.AreEqual("projectionStart", exception.ParamName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankName_Throws(string? name)
    {
        Assert.ThrowsExactly<ArgumentException>(() => NewGoal(name: name!));
    }

    [TestMethod]
    public void Constructor_NullTargetAmount_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new FinancialGoal(
                "Goal",
                null!,
                new MoneyAmount(1000m),
                new DateOnly(2026, 12, 31),
                "Purpose"));

        Assert.AreEqual("targetAmount", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_NullCurrentAmount_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new FinancialGoal(
                "Goal",
                new MoneyAmount(1000m),
                null!,
                new DateOnly(2026, 12, 31),
                "Purpose"));

        Assert.AreEqual("currentAmount", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_MixedCurrencies_Throws()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() =>
            NewGoal(currentAmount: new MoneyAmount(1000m, "USD")));

        Assert.AreEqual("currentAmount", exception.ParamName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankPurpose_Throws(string? purpose)
    {
        Assert.ThrowsExactly<ArgumentException>(() => NewGoal(purpose: purpose!));
    }

    private static FinancialGoal NewGoal(
        string name = "Goal",
        MoneyAmount? targetAmount = null,
        MoneyAmount? currentAmount = null,
        DateOnly? targetDate = null,
        string purpose = "Purpose") =>
        new(
            name,
            targetAmount ?? new MoneyAmount(100000m),
            currentAmount ?? new MoneyAmount(25000m),
            targetDate ?? new DateOnly(2026, 12, 31),
            purpose);
}