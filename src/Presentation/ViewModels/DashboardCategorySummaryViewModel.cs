using System.Globalization;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Presentation.ViewModels;

public sealed record DashboardCategorySummaryViewModel(
    string Category,
    string ExpensesText)
{
    public static DashboardCategorySummaryViewModel FromSummary(CategoryMonthlySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new DashboardCategorySummaryViewModel(
            summary.Category,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{summary.Expenses.Amount:0.00} {summary.Expenses.Currency}"));
    }
}