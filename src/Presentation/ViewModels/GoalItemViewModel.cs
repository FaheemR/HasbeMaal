using System.Globalization;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed record GoalItemViewModel(
    Guid Id,
    string Name,
    string Purpose,
    string TargetText,
    string CurrentText,
    string RemainingText,
    string TargetDateText,
    string MonthlyContributionText,
    string StatusText,
    decimal Progress)
{
    public static GoalItemViewModel FromGoal(FinancialGoal goal, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(goal);

        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var targetMonth = new DateOnly(goal.TargetDate.Year, goal.TargetDate.Month, 1);
        var currency = goal.TargetAmount.Currency;
        var remaining = Math.Max(goal.TargetAmount.Amount - goal.CurrentAmount.Amount, 0m);
        var isReached = remaining == 0m;
        var isOverdue = targetMonth < thisMonth && !isReached;

        string monthlyContributionText;
        string statusText;
        if (isReached)
        {
            monthlyContributionText = "No monthly set-aside needed";
            statusText = "Goal reached";
        }
        else if (isOverdue)
        {
            monthlyContributionText = $"{FormatMoney(remaining, currency)} still needed";
            statusText = "Target date passed";
        }
        else
        {
            var projection = goal.CreateMonthlyContributionProjection(today);
            monthlyContributionText =
                $"Set aside {FormatMoney(projection.RequiredMonthlyContribution.Amount, currency)}/mo";
            statusText = projection.MonthsRemaining == 1
                ? "1 month left"
                : $"{projection.MonthsRemaining} months left";
        }

        var progress = goal.TargetAmount.Amount <= 0m
            ? 1m
            : Math.Clamp(goal.CurrentAmount.Amount / goal.TargetAmount.Amount, 0m, 1m);

        return new GoalItemViewModel(
            goal.Id,
            goal.Name,
            goal.Purpose,
            $"Target {FormatMoney(goal.TargetAmount.Amount, currency)}",
            $"Saved {FormatMoney(goal.CurrentAmount.Amount, currency)}",
            $"{FormatMoney(remaining, currency)} to go",
            $"By {goal.TargetDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)}",
            monthlyContributionText,
            statusText,
            progress);
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:0.00} {currency}");
    }
}
