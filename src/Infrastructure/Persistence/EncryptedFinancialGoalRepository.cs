using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;

namespace HasbeMaal.Infrastructure.Persistence;

public sealed class EncryptedFinancialGoalRepository : IFinancialGoalRepository
{
    private const string PartitionKey = "financial-goals:v1";

    private readonly IEncryptedStore store;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public EncryptedFinancialGoalRepository(IEncryptedStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        this.store = store;
    }

    public async Task<IReadOnlyList<FinancialGoal>> ListAsync(CancellationToken cancellationToken = default)
    {
        var stored = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
        return stored.Select(ToGoal).ToArray();
    }

    public async Task SaveAsync(FinancialGoal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var goals = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
            var updated = ToStored(goal);
            var index = goals.FindIndex(existing => existing.Id == goal.Id);
            if (index >= 0)
            {
                goals[index] = updated;
            }
            else
            {
                goals.Add(updated);
            }

            await SaveStoredAsync(goals, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var goals = await LoadStoredAsync(cancellationToken).ConfigureAwait(false);
            if (goals.RemoveAll(existing => existing.Id == id) == 0)
            {
                return;
            }

            await SaveStoredAsync(goals, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private async Task<List<StoredFinancialGoal>> LoadStoredAsync(CancellationToken cancellationToken)
    {
        var stored = await store.LoadAsync<StoredFinancialGoals>(PartitionKey, cancellationToken).ConfigureAwait(false);
        return stored is null ? [] : [.. stored.Goals];
    }

    private async Task SaveStoredAsync(List<StoredFinancialGoal> goals, CancellationToken cancellationToken)
    {
        await store.SaveAsync(PartitionKey, new StoredFinancialGoals(goals.ToArray()), cancellationToken)
            .ConfigureAwait(false);
    }

    private static FinancialGoal ToGoal(StoredFinancialGoal stored)
    {
        return new FinancialGoal(
            stored.Id,
            stored.Name,
            new MoneyAmount(stored.TargetAmount, stored.Currency),
            new MoneyAmount(stored.CurrentAmount, stored.Currency),
            stored.TargetDate,
            stored.Purpose);
    }

    private static StoredFinancialGoal ToStored(FinancialGoal goal)
    {
        return new StoredFinancialGoal(
            goal.Id,
            goal.Name,
            goal.TargetAmount.Amount,
            goal.CurrentAmount.Amount,
            goal.TargetAmount.Currency,
            goal.TargetDate,
            goal.Purpose);
    }

    private sealed record StoredFinancialGoals(StoredFinancialGoal[] Goals);

    private sealed record StoredFinancialGoal(
        Guid Id,
        string Name,
        decimal TargetAmount,
        decimal CurrentAmount,
        string Currency,
        DateOnly TargetDate,
        string Purpose);
}
