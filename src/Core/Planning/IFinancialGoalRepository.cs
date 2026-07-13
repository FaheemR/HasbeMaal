namespace HasbeMaal.Core.Planning;

public interface IFinancialGoalRepository
{
    Task<IReadOnlyList<FinancialGoal>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(FinancialGoal goal, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
