namespace HasbeMaal.Core.Planning;

public sealed record MonthlyBudgetCategories
{
    public MonthlyBudgetCategories(
        int year,
        int month,
        IEnumerable<BudgetCategory> categories)
    {
        ValidatePeriod(year, month);
        ArgumentNullException.ThrowIfNull(categories);

        var snapshot = categories.ToArray();
        if (snapshot.Any(category => category is null))
        {
            throw new ArgumentException("Budget category list cannot contain null values.", nameof(categories));
        }

        if (snapshot
            .GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Budget category names must be unique within a month.", nameof(categories));
        }

        if (snapshot
            .Select(category => category.MonthlyLimit.Currency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() > 1)
        {
            throw new ArgumentException("Monthly budget categories cannot mix currencies.", nameof(categories));
        }

        Year = year;
        Month = month;
        Categories = snapshot
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public int Year { get; }

    public int Month { get; }

    public IReadOnlyList<BudgetCategory> Categories { get; }

    public static MonthlyBudgetCategories Empty(int year, int month)
    {
        return new MonthlyBudgetCategories(year, month, []);
    }

    public static void ValidatePeriod(int year, int month)
    {
        if (year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be positive.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }
    }
}