using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;
using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class EncryptedFinancialGoalRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_ThenListAsync_RoundTripsGoal()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var goal = new FinancialGoal(
            "Emergency fund",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            new DateOnly(2027, 6, 30),
            "Household reserve");

        await repository.SaveAsync(goal);

        var loaded = Assert.ContainsSingle(await repository.ListAsync());
        Assert.AreEqual(goal.Id, loaded.Id);
        Assert.AreEqual("Emergency fund", loaded.Name);
        Assert.AreEqual(new MoneyAmount(150000m), loaded.TargetAmount);
        Assert.AreEqual(new MoneyAmount(25000m), loaded.CurrentAmount);
        Assert.AreEqual(new DateOnly(2027, 6, 30), loaded.TargetDate);
        Assert.AreEqual("Household reserve", loaded.Purpose);
    }

    [TestMethod]
    public async Task SaveAsync_ExistingId_UpdatesGoalInPlace()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var id = Guid.NewGuid();
        var original = new FinancialGoal(
            id,
            "School admission",
            new MoneyAmount(80000m),
            new MoneyAmount(20000m),
            new DateOnly(2027, 6, 1),
            "Kid school");
        var updated = new FinancialGoal(
            id,
            "School admission",
            new MoneyAmount(80000m),
            new MoneyAmount(50000m),
            new DateOnly(2027, 6, 1),
            "Kid school");

        await repository.SaveAsync(original);
        await repository.SaveAsync(updated);

        var loaded = Assert.ContainsSingle(await repository.ListAsync());
        Assert.AreEqual(id, loaded.Id);
        Assert.AreEqual(new MoneyAmount(50000m), loaded.CurrentAmount);
    }

    [TestMethod]
    public async Task SaveAsync_DifferentIds_PersistsSeparateGoals()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var emergencyFund = new FinancialGoal(
            "Emergency fund",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            new DateOnly(2027, 6, 30),
            "Household reserve");
        var admission = new FinancialGoal(
            "School admission",
            new MoneyAmount(80000m),
            new MoneyAmount(20000m),
            new DateOnly(2027, 6, 1),
            "Kid school");

        await repository.SaveAsync(emergencyFund);
        await repository.SaveAsync(admission);

        var loaded = await repository.ListAsync();

        Assert.HasCount(2, loaded);
        Assert.ContainsSingle(goal => goal.Id == emergencyFund.Id, loaded);
        Assert.ContainsSingle(goal => goal.Id == admission.Id, loaded);
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingId_RemovesGoal()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var emergencyFund = new FinancialGoal(
            "Emergency fund",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            new DateOnly(2027, 6, 30),
            "Household reserve");
        var admission = new FinancialGoal(
            "School admission",
            new MoneyAmount(80000m),
            new MoneyAmount(20000m),
            new DateOnly(2027, 6, 1),
            "Kid school");
        await repository.SaveAsync(emergencyFund);
        await repository.SaveAsync(admission);

        await repository.DeleteAsync(emergencyFund.Id);

        var remaining = Assert.ContainsSingle(await repository.ListAsync());
        Assert.AreEqual(admission.Id, remaining.Id);
    }

    [TestMethod]
    public async Task DeleteAsync_UnknownId_LeavesGoalsUnchanged()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var goal = new FinancialGoal(
            "Emergency fund",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            new DateOnly(2027, 6, 30),
            "Household reserve");
        await repository.SaveAsync(goal);

        await repository.DeleteAsync(Guid.NewGuid());

        var remaining = Assert.ContainsSingle(await repository.ListAsync());
        Assert.AreEqual(goal.Id, remaining.Id);
    }

    [TestMethod]
    public async Task ListAsync_WhenNothingStored_ReturnsEmpty()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        Assert.IsEmpty(await repository.ListAsync());
    }

    [TestMethod]
    public async Task SaveAsync_NullGoal_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await repository.SaveAsync(null!));
    }

    [TestMethod]
    public async Task SaveAsync_DoesNotWritePlaintextGoalFields()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var goal = new FinancialGoal(
            "Emergency fund",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            new DateOnly(2027, 6, 30),
            "Household reserve");

        await repository.SaveAsync(goal);

        var file = Assert.ContainsSingle(Directory.GetFiles(directory.Path));
        var contents = await File.ReadAllTextAsync(file);

        Assert.DoesNotContain("Emergency fund", contents);
        Assert.DoesNotContain("Household reserve", contents);
        Assert.DoesNotContain("150000", contents);
    }

    private static EncryptedFinancialGoalRepository NewRepository(string directory)
    {
        return new EncryptedFinancialGoalRepository(NewStore(directory));
    }

    private static FileEncryptedStore NewStore(string directory)
    {
        var key = Enumerable.Repeat(0x42, 32).Select(value => (byte)value).ToArray();
        return new FileEncryptedStore(directory, key);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hasbemaal-goal-repository-test-{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
