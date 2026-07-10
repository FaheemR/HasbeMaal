using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record ScenarioAssumptions
{
    public ScenarioAssumptions(
        string name,
        int startYear,
        int startMonth,
        int horizonMonths,
        MoneyAmount monthlyIncome,
        MoneyAmount essentialMonthlyExpenses,
        MoneyAmount discretionaryMonthlyExpenses,
        MoneyAmount monthlyDebtPayments,
        MoneyAmount monthlyFamilySupport,
        MoneyAmount monthlyGoalContributions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Scenario name is required.", nameof(name));
        }

        MonthlyBudgetCategories.ValidatePeriod(startYear, startMonth);

        if (horizonMonths < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonMonths), "Scenario horizon must be at least one month.");
        }

        ArgumentNullException.ThrowIfNull(monthlyIncome);
        ArgumentNullException.ThrowIfNull(essentialMonthlyExpenses);
        ArgumentNullException.ThrowIfNull(discretionaryMonthlyExpenses);
        ArgumentNullException.ThrowIfNull(monthlyDebtPayments);
        ArgumentNullException.ThrowIfNull(monthlyFamilySupport);
        ArgumentNullException.ThrowIfNull(monthlyGoalContributions);

        var currency = monthlyIncome.Currency;
        ValidateCurrency(essentialMonthlyExpenses, currency, nameof(essentialMonthlyExpenses));
        ValidateCurrency(discretionaryMonthlyExpenses, currency, nameof(discretionaryMonthlyExpenses));
        ValidateCurrency(monthlyDebtPayments, currency, nameof(monthlyDebtPayments));
        ValidateCurrency(monthlyFamilySupport, currency, nameof(monthlyFamilySupport));
        ValidateCurrency(monthlyGoalContributions, currency, nameof(monthlyGoalContributions));

        Name = name.Trim();
        StartYear = startYear;
        StartMonth = startMonth;
        HorizonMonths = horizonMonths;
        MonthlyIncome = monthlyIncome;
        EssentialMonthlyExpenses = essentialMonthlyExpenses;
        DiscretionaryMonthlyExpenses = discretionaryMonthlyExpenses;
        MonthlyDebtPayments = monthlyDebtPayments;
        MonthlyFamilySupport = monthlyFamilySupport;
        MonthlyGoalContributions = monthlyGoalContributions;

        var totalOutflow = essentialMonthlyExpenses.Amount
            + discretionaryMonthlyExpenses.Amount
            + monthlyDebtPayments.Amount
            + monthlyFamilySupport.Amount
            + monthlyGoalContributions.Amount;

        TotalMonthlyOutflow = new MoneyAmount(totalOutflow, currency);
        MonthlySurplus = new MoneyAmount(Math.Max(monthlyIncome.Amount - totalOutflow, 0m), currency);
        MonthlyShortfall = new MoneyAmount(Math.Max(totalOutflow - monthlyIncome.Amount, 0m), currency);
    }

    public string Name { get; }

    public int StartYear { get; }

    public int StartMonth { get; }

    public int HorizonMonths { get; }

    public MoneyAmount MonthlyIncome { get; }

    public MoneyAmount EssentialMonthlyExpenses { get; }

    public MoneyAmount DiscretionaryMonthlyExpenses { get; }

    public MoneyAmount MonthlyDebtPayments { get; }

    public MoneyAmount MonthlyFamilySupport { get; }

    public MoneyAmount MonthlyGoalContributions { get; }

    public DateOnly PeriodStart => new(StartYear, StartMonth, 1);

    public DateOnly PeriodEnd => PeriodStart.AddMonths(HorizonMonths - 1);

    public MoneyAmount TotalMonthlyOutflow { get; }

    public MoneyAmount MonthlySurplus { get; }

    public MoneyAmount MonthlyShortfall { get; }

    private static void ValidateCurrency(MoneyAmount amount, string expectedCurrency, string paramName)
    {
        if (amount.Currency != expectedCurrency)
        {
            throw new ArgumentException("Scenario assumptions cannot mix currencies.", paramName);
        }
    }
}