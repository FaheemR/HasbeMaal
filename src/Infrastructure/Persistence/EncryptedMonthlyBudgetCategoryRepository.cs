using HasbeMaal.Core.Planning;

namespace HasbeMaal.Infrastructure.Persistence;

public sealed class EncryptedMonthlyBudgetCategoryRepository : IMonthlyBudgetCategoryRepository
{
    private const string PartitionPrefix = "monthly-budget-categories:v1";

    private readonly IEncryptedStore store;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public EncryptedMonthlyBudgetCategoryRepository(IEncryptedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public async Task SaveAsync(
        MonthlyBudgetCategories budgetCategories,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budgetCategories);

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stored = new StoredMonthlyBudgetCategories(budgetCategories.Categories.ToArray());

            await store.SaveAsync(
                    GetPartitionKey(budgetCategories.Year, budgetCategories.Month),
                    stored,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<MonthlyBudgetCategories> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        MonthlyBudgetCategories.ValidatePeriod(year, month);

        var stored = await store.LoadAsync<StoredMonthlyBudgetCategories>(
                GetPartitionKey(year, month),
                cancellationToken)
            .ConfigureAwait(false);

        return stored is null
            ? MonthlyBudgetCategories.Empty(year, month)
            : new MonthlyBudgetCategories(year, month, stored.Categories);
    }

    private static string GetPartitionKey(int year, int month)
    {
        return $"{PartitionPrefix}:{year:D4}-{month:D2}";
    }

    private sealed record StoredMonthlyBudgetCategories(BudgetCategory[] Categories);
}