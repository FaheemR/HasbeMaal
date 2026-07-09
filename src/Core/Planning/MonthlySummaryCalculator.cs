using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public static class MonthlySummaryCalculator
{
    public static MonthlyFinancialSummary Calculate(
        IEnumerable<FinancialTransaction> transactions,
        int year,
        int month)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be positive.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        var monthlyTransactions = transactions
            .Where(transaction => transaction.OccurredAt.Year == year && transaction.OccurredAt.Month == month)
            .ToArray();

        var currency = monthlyTransactions.FirstOrDefault()?.Amount.Currency ?? "INR";
        if (monthlyTransactions.Any(transaction => transaction.Amount.Currency != currency))
        {
            throw new InvalidOperationException("Monthly summary cannot mix currencies.");
        }

        var income = monthlyTransactions
            .Where(transaction => transaction.Direction == TransactionDirection.Credit)
            .Sum(transaction => transaction.Amount.Amount);
        var expenses = monthlyTransactions
            .Where(transaction => transaction.Direction == TransactionDirection.Debit)
            .Sum(transaction => transaction.Amount.Amount);
        var categories = monthlyTransactions
            .Where(transaction => transaction.Direction == TransactionDirection.Debit)
            .GroupBy(transaction => transaction.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryMonthlySummary(
                group.First().Category,
                new MoneyAmount(group.Sum(transaction => transaction.Amount.Amount), currency)))
            .ToArray();

        return new MonthlyFinancialSummary(
            year,
            month,
            new MoneyAmount(income, currency),
            new MoneyAmount(expenses, currency),
            new MoneyAmount(Math.Max(income - expenses, 0m), currency),
            new MoneyAmount(Math.Max(expenses - income, 0m), currency),
            categories);
    }
}