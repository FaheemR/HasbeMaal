using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;
using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class EncryptedMonthlyBudgetCategoryRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_ThenGetAsync_RoundTripsBudgetCategories()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var budget = new MonthlyBudgetCategories(
            2026,
            7,
            [
                new BudgetCategory("Groceries", new MoneyAmount(1000.25m), isEssential: true),
                new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false),
            ]);

        await repository.SaveAsync(budget);

        var loaded = await repository.GetAsync(2026, 7);

        Assert.AreEqual(2026, loaded.Year);
        Assert.AreEqual(7, loaded.Month);
        CollectionAssert.AreEqual(
            budget.Categories.ToArray(),
            loaded.Categories.ToArray());
    }

    [TestMethod]
    public async Task SaveAsync_ExistingMonth_ReplacesCategories()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var original = new MonthlyBudgetCategories(
            2026,
            7,
            [new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true)]);
        var updated = new MonthlyBudgetCategories(
            2026,
            7,
            [new BudgetCategory("Education", new MoneyAmount(2500m), isEssential: true)]);

        await repository.SaveAsync(original);
        await repository.SaveAsync(updated);

        var loaded = await repository.GetAsync(2026, 7);

        var category = Assert.ContainsSingle(loaded.Categories);
        Assert.AreEqual("Education", category.Name);
        Assert.AreEqual(new MoneyAmount(2500m), category.MonthlyLimit);
    }

    [TestMethod]
    public async Task SaveAsync_DifferentMonths_PersistsSeparateBudgets()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var july = new MonthlyBudgetCategories(
            2026,
            7,
            [new BudgetCategory("Groceries", new MoneyAmount(1000m), isEssential: true)]);
        var august = new MonthlyBudgetCategories(
            2026,
            8,
            [new BudgetCategory("Transport", new MoneyAmount(500m), isEssential: false)]);

        await repository.SaveAsync(july);
        await repository.SaveAsync(august);

        var loadedJuly = await repository.GetAsync(2026, 7);
        var loadedAugust = await repository.GetAsync(2026, 8);

        Assert.AreEqual("Groceries", Assert.ContainsSingle(loadedJuly.Categories).Name);
        Assert.AreEqual("Transport", Assert.ContainsSingle(loadedAugust.Categories).Name);
    }

    [TestMethod]
    public async Task GetAsync_MissingMonth_ReturnsEmptyBudget()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        var loaded = await repository.GetAsync(2026, 7);

        Assert.AreEqual(2026, loaded.Year);
        Assert.AreEqual(7, loaded.Month);
        Assert.IsEmpty(loaded.Categories);
    }

    [TestMethod]
    public async Task SaveAsync_NullBudget_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await repository.SaveAsync(null!));
    }

    [TestMethod]
    [DataRow(0, 7, "year")]
    [DataRow(2026, 0, "month")]
    [DataRow(2026, 13, "month")]
    public async Task GetAsync_InvalidPeriod_Throws(int year, int month, string expectedParamName)
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        var exception = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await repository.GetAsync(year, month));

        Assert.AreEqual(expectedParamName, exception.ParamName);
    }

    [TestMethod]
    public async Task SaveAsync_DoesNotWritePlaintextBudgetFields()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var budget = new MonthlyBudgetCategories(
            2026,
            7,
            [new BudgetCategory("Groceries", new MoneyAmount(1000.25m), isEssential: true)]);

        await repository.SaveAsync(budget);

        var file = Assert.ContainsSingle(Directory.GetFiles(directory.Path));
        var contents = await File.ReadAllTextAsync(file);

        Assert.DoesNotContain("Groceries", contents);
        Assert.DoesNotContain("1000.25", contents);
    }

    private static EncryptedMonthlyBudgetCategoryRepository NewRepository(string directory)
    {
        return new EncryptedMonthlyBudgetCategoryRepository(NewStore(directory));
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
                $"hasbemaal-budget-repository-test-{Guid.NewGuid():N}");

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