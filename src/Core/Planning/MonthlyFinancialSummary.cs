using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record MonthlyFinancialSummary(
    int Year,
    int Month,
    MoneyAmount Income,
    MoneyAmount Expenses,
    MoneyAmount Surplus,
    MoneyAmount Overspend,
    IReadOnlyList<CategoryMonthlySummary> Categories);