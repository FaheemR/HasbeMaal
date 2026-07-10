namespace HasbeMaal.Core.Domain;

public static class RecurringTransactionDetector
{
    public static IReadOnlyList<RecurringTransactionPattern> DetectMonthlyPatterns(
        IEnumerable<FinancialTransaction> transactions,
        int minimumOccurrences = 3,
        int dayTolerance = 3)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (minimumOccurrences < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOccurrences), "At least two occurrences are required.");
        }

        if (dayTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayTolerance), "Day tolerance cannot be negative.");
        }

        return transactions
            .GroupBy(TransactionRecurrenceKey.FromTransaction)
            .Select(group => TryCreatePattern(group, minimumOccurrences, dayTolerance))
            .Where(pattern => pattern is not null)
            .Select(pattern => pattern!)
            .OrderBy(pattern => pattern.Merchant, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pattern => pattern.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pattern => pattern.Amount.Amount)
            .ThenBy(pattern => pattern.Direction)
            .ToArray();
    }

    private static RecurringTransactionPattern? TryCreatePattern(
        IEnumerable<FinancialTransaction> transactions,
        int minimumOccurrences,
        int dayTolerance)
    {
        var monthlyOccurrences = transactions
            .GroupBy(transaction => new { transaction.OccurredAt.Year, transaction.OccurredAt.Month })
            .Select(group => group.OrderBy(transaction => transaction.OccurredAt).First())
            .OrderBy(transaction => transaction.OccurredAt)
            .ToArray();

        if (monthlyOccurrences.Length < minimumOccurrences)
        {
            return null;
        }

        if (!AreConsecutiveMonths(monthlyOccurrences) || !AreDaysWithinTolerance(monthlyOccurrences, dayTolerance))
        {
            return null;
        }

        var first = monthlyOccurrences[0];
        var last = monthlyOccurrences[^1];

        return new RecurringTransactionPattern(
            first.Merchant,
            first.Category,
            first.Direction,
            first.Amount,
            ToDateOnly(first.OccurredAt),
            ToDateOnly(last.OccurredAt),
            monthlyOccurrences.Length,
            first.OccurredAt.Day);
    }

    private static bool AreConsecutiveMonths(IReadOnlyList<FinancialTransaction> transactions)
    {
        var firstMonth = new DateOnly(transactions[0].OccurredAt.Year, transactions[0].OccurredAt.Month, 1);

        for (var index = 1; index < transactions.Count; index++)
        {
            var expectedMonth = firstMonth.AddMonths(index);
            var actualMonth = new DateOnly(transactions[index].OccurredAt.Year, transactions[index].OccurredAt.Month, 1);

            if (actualMonth != expectedMonth)
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreDaysWithinTolerance(IReadOnlyList<FinancialTransaction> transactions, int dayTolerance)
    {
        var expectedDay = transactions[0].OccurredAt.Day;

        return transactions.All(transaction => Math.Abs(transaction.OccurredAt.Day - expectedDay) <= dayTolerance);
    }

    private static DateOnly ToDateOnly(DateTimeOffset value)
    {
        return new DateOnly(value.Year, value.Month, value.Day);
    }

    private sealed record TransactionRecurrenceKey(
        string Merchant,
        string Category,
        TransactionDirection Direction,
        decimal Amount,
        string Currency)
    {
        public static TransactionRecurrenceKey FromTransaction(FinancialTransaction transaction)
        {
            return new TransactionRecurrenceKey(
                transaction.Merchant.ToUpperInvariant(),
                transaction.Category.ToUpperInvariant(),
                transaction.Direction,
                transaction.Amount.Amount,
                transaction.Amount.Currency);
        }
    }
}