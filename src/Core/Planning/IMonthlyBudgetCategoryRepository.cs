namespace HasbeMaal.Core.Planning;

public interface IMonthlyBudgetCategoryRepository
{
    Task SaveAsync(MonthlyBudgetCategories budgetCategories, CancellationToken cancellationToken = default);

    Task<MonthlyBudgetCategories> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}