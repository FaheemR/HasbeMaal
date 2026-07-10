using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record GoalContributionProjection(
    string GoalName,
    DateOnly ProjectionStartMonth,
    DateOnly TargetMonth,
    int MonthsRemaining,
    MoneyAmount RemainingAmount,
    MoneyAmount RequiredMonthlyContribution,
    bool IsTargetReached);