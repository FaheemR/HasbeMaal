using System.Globalization;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed record BudgetProgressItemViewModel(
    string CategoryName,
    string CategoryTypeText,
    string SpentText,
    string LimitText,
    string RemainingText,
    string StatusText,
    decimal Progress)
{
    public static BudgetProgressItemViewModel FromCategory(BudgetCategory category, MoneyAmount spent)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(spent);

        var evaluation = category.EvaluateMonthlySpent(spent);
        var statusText = evaluation.Overspend.Amount > 0m
            ? $"Over limit by {FormatMoney(evaluation.Overspend)}"
            : $"{FormatMoney(evaluation.Remaining)} remaining";

        return new BudgetProgressItemViewModel(
            evaluation.CategoryName,
            evaluation.IsEssential ? "Essential" : "Flexible",
            $"Spent {FormatMoney(evaluation.Spent)}",
            $"Limit {FormatMoney(evaluation.MonthlyLimit)}",
            FormatPercent(evaluation.PercentUsed),
            statusText,
            Math.Clamp(evaluation.PercentUsed / 100m, 0m, 1m));
    }

    private static string FormatMoney(MoneyAmount amount)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{amount.Amount:0.00} {amount.Currency}");
    }

    private static string FormatPercent(decimal percentUsed)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{percentUsed:0.#}% used");
    }
}