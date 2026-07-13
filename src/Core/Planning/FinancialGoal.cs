using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record FinancialGoal
{
    public FinancialGoal(
        string name,
        MoneyAmount targetAmount,
        MoneyAmount currentAmount,
        DateOnly targetDate,
        string purpose)
        : this(Guid.NewGuid(), name, targetAmount, currentAmount, targetDate, purpose)
    {
    }

    public FinancialGoal(
        Guid id,
        string name,
        MoneyAmount targetAmount,
        MoneyAmount currentAmount,
        DateOnly targetDate,
        string purpose)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Goal id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Goal name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(targetAmount);
        ArgumentNullException.ThrowIfNull(currentAmount);

        if (targetAmount.Currency != currentAmount.Currency)
        {
            throw new ArgumentException("Goal target and current amounts must use the same currency.", nameof(currentAmount));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Goal purpose is required.", nameof(purpose));
        }

        Id = id;
        Name = name.Trim();
        TargetAmount = targetAmount;
        CurrentAmount = currentAmount;
        TargetDate = targetDate;
        Purpose = purpose.Trim();
    }

    public Guid Id { get; }

    public string Name { get; }

    public MoneyAmount TargetAmount { get; }

    public MoneyAmount CurrentAmount { get; }

    public DateOnly TargetDate { get; }

    public string Purpose { get; }

    public GoalContributionProjection CreateMonthlyContributionProjection(DateOnly projectionStart)
    {
        var projectionStartMonth = new DateOnly(projectionStart.Year, projectionStart.Month, 1);
        var targetMonth = new DateOnly(TargetDate.Year, TargetDate.Month, 1);
        var remainingAmount = Math.Max(TargetAmount.Amount - CurrentAmount.Amount, 0m);
        var isTargetReached = remainingAmount == 0m;

        if (targetMonth < projectionStartMonth && !isTargetReached)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectionStart),
                "Projection start must not be after the target month when the goal still has a remaining amount.");
        }

        var monthsRemaining = targetMonth < projectionStartMonth
            ? 0
            : CountMonthsInclusive(projectionStartMonth, targetMonth);
        var requiredMonthlyContribution = isTargetReached
            ? 0m
            : remainingAmount / monthsRemaining;

        return new GoalContributionProjection(
            Name,
            projectionStartMonth,
            targetMonth,
            monthsRemaining,
            new MoneyAmount(remainingAmount, TargetAmount.Currency),
            new MoneyAmount(requiredMonthlyContribution, TargetAmount.Currency),
            isTargetReached);
    }

    private static int CountMonthsInclusive(DateOnly startMonth, DateOnly endMonth)
    {
        return ((endMonth.Year - startMonth.Year) * 12) + endMonth.Month - startMonth.Month + 1;
    }
}