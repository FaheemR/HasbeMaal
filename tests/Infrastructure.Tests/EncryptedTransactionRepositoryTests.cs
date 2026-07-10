using HasbeMaal.Core.Domain;
using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class EncryptedTransactionRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_ThenGetByIdAsync_RoundTripsTransaction()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var transaction = NewTransaction(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero),
            merchant: "REDACTED STORE",
            category: "Groceries",
            amount: 125.75m);

        await repository.SaveAsync(transaction);

        var loaded = await repository.GetByIdAsync(transaction.Id);

        Assert.AreEqual(transaction, loaded);
    }

    [TestMethod]
    public async Task SaveAsync_ExistingId_UpsertsWithoutDuplicate()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var original = NewTransaction(
            id,
            new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero),
            merchant: "REDACTED STORE",
            category: "Groceries",
            amount: 125.75m);
        var updated = NewTransaction(
            id,
            new DateTimeOffset(2026, 7, 10, 8, 15, 0, TimeSpan.Zero),
            merchant: "REDACTED SCHOOL",
            category: "Education",
            amount: 450m);

        await repository.SaveAsync(original);
        await repository.SaveAsync(updated);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        var transaction = Assert.ContainsSingle(transactions);
        Assert.AreEqual(updated, transaction);
    }

    [TestMethod]
    public async Task SaveAsync_ConcurrentSavesThroughSameInstance_PersistsAllTransactions()
    {
        var repository = new EncryptedTransactionRepository(new DelayedInMemoryEncryptedStore());
        var transactions = Enumerable.Range(1, 20)
            .Select(index => NewTransaction(
                Guid.Parse($"55555555-5555-5555-5555-{index:000000000000}"),
                new DateTimeOffset(2026, 7, 9, 12, index, 0, TimeSpan.Zero),
                merchant: $"REDACTED STORE {index}",
                category: "Groceries",
                amount: 10m + index))
            .ToArray();

        await Task.WhenAll(transactions.Select(transaction => repository.SaveAsync(transaction)));

        var persisted = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        CollectionAssert.AreEquivalent(
            transactions.Select(transaction => transaction.Id).ToArray(),
            persisted.Select(transaction => transaction.Id).ToArray());
    }

    [TestMethod]
    public async Task GetByIdAsync_MissingTransaction_ReturnsNull()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        var loaded = await repository.GetByIdAsync(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        Assert.IsNull(loaded);
    }

    [TestMethod]
    public async Task SaveManyAsync_PersistsAllTransactions()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var first = NewTransaction(
            Guid.Parse("77777777-7777-7777-7777-000000000001"),
            new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED STORE",
            category: "Groceries",
            amount: 100m);
        var second = NewTransaction(
            Guid.Parse("77777777-7777-7777-7777-000000000002"),
            new DateTimeOffset(2026, 7, 9, 11, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED SCHOOL",
            category: "Education",
            amount: 200m);

        await repository.SaveManyAsync([first, second]);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        CollectionAssert.AreEquivalent(
            new[] { first.Id, second.Id },
            transactions.Select(transaction => transaction.Id).ToArray());
    }

    [TestMethod]
    public async Task SaveManyAsync_ExistingId_UpsertsWithoutDuplicate()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var id = Guid.Parse("77777777-7777-7777-7777-000000000003");
        var original = NewTransaction(
            id,
            new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED STORE",
            category: "Groceries",
            amount: 100m);
        var updated = NewTransaction(
            id,
            new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED SCHOOL",
            category: "Education",
            amount: 250m);

        await repository.SaveAsync(original);
        await repository.SaveManyAsync([updated]);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        var transaction = Assert.ContainsSingle(transactions);
        Assert.AreEqual(updated, transaction);
    }

    [TestMethod]
    public async Task SaveManyAsync_NullTransactions_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await repository.SaveManyAsync(null!));
    }

    [TestMethod]
    public async Task SaveManyAsync_EmptyBatch_PersistsNothing()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        await repository.SaveManyAsync([]);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        Assert.IsEmpty(transactions);
    }

    [TestMethod]
    public async Task ListAsync_InclusiveDateRange_ReturnsDeterministicOrder()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var outsideBefore = NewTransaction(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            new DateTimeOffset(2026, 6, 30, 23, 59, 0, TimeSpan.Zero),
            merchant: "REDACTED OUTSIDE BEFORE",
            category: "Other",
            amount: 10m);
        var fromBoundary = NewTransaction(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED FROM",
            category: "Groceries",
            amount: 20m);
        var sameTimestampSecondId = NewTransaction(
            Guid.Parse("00000000-0000-0000-0000-000000000004"),
            new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED LUNCH",
            category: "Food",
            amount: 30m);
        var sameTimestampFirstId = NewTransaction(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED TEA",
            category: "Food",
            amount: 40m);
        var toBoundary = NewTransaction(
            Guid.Parse("00000000-0000-0000-0000-000000000005"),
            new DateTimeOffset(2026, 7, 31, 23, 59, 0, TimeSpan.Zero),
            merchant: "REDACTED TO",
            category: "Utilities",
            amount: 50m);
        var outsideAfter = NewTransaction(
            Guid.Parse("00000000-0000-0000-0000-000000000006"),
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            merchant: "REDACTED OUTSIDE AFTER",
            category: "Other",
            amount: 60m);

        await repository.SaveAsync(outsideBefore);
        await repository.SaveAsync(fromBoundary);
        await repository.SaveAsync(sameTimestampSecondId);
        await repository.SaveAsync(toBoundary);
        await repository.SaveAsync(sameTimestampFirstId);
        await repository.SaveAsync(outsideAfter);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        CollectionAssert.AreEqual(
            new[]
            {
                toBoundary.Id,
                sameTimestampFirstId.Id,
                sameTimestampSecondId.Id,
                fromBoundary.Id,
            },
            transactions.Select(transaction => transaction.Id).ToArray());
    }

    [TestMethod]
    public async Task ListAsync_UsesTransactionOffsetDateAtBoundaries()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var includedAtStartBoundary = NewTransaction(
            Guid.Parse("66666666-6666-6666-6666-000000000001"),
            new DateTimeOffset(2026, 7, 9, 0, 15, 0, TimeSpan.FromHours(14)),
            merchant: "REDACTED EARLY OFFSET",
            category: "Travel",
            amount: 10m);
        var includedAtEndBoundary = NewTransaction(
            Guid.Parse("66666666-6666-6666-6666-000000000002"),
            new DateTimeOffset(2026, 7, 9, 23, 45, 0, TimeSpan.FromHours(-10)),
            merchant: "REDACTED LATE OFFSET",
            category: "Utilities",
            amount: 20m);
        var excludedPreviousOffsetDate = NewTransaction(
            Guid.Parse("66666666-6666-6666-6666-000000000003"),
            new DateTimeOffset(2026, 7, 8, 23, 45, 0, TimeSpan.FromHours(-10)),
            merchant: "REDACTED PREVIOUS OFFSET DATE",
            category: "Other",
            amount: 30m);
        var excludedNextOffsetDate = NewTransaction(
            Guid.Parse("66666666-6666-6666-6666-000000000004"),
            new DateTimeOffset(2026, 7, 10, 0, 15, 0, TimeSpan.FromHours(14)),
            merchant: "REDACTED NEXT OFFSET DATE",
            category: "Other",
            amount: 40m);

        await repository.SaveAsync(excludedPreviousOffsetDate);
        await repository.SaveAsync(includedAtStartBoundary);
        await repository.SaveAsync(includedAtEndBoundary);
        await repository.SaveAsync(excludedNextOffsetDate);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 9),
            new DateOnly(2026, 7, 9));

        CollectionAssert.AreEquivalent(
            new[] { includedAtStartBoundary.Id, includedAtEndBoundary.Id },
            transactions.Select(transaction => transaction.Id).ToArray());
    }

    [TestMethod]
    public async Task ListAsync_EmptyStore_ReturnsEmptyList()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        var transactions = await repository.ListAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31));

        Assert.IsEmpty(transactions);
    }

    [TestMethod]
    public async Task ListAsync_FromAfterTo_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await repository.ListAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 7, 31)));

        Assert.AreEqual("from", exception.ParamName);
    }

    [TestMethod]
    public async Task SaveAsync_NullTransaction_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await repository.SaveAsync(null!));
    }

    [TestMethod]
    public async Task SaveAsync_DoesNotWritePlaintextTransactionFields()
    {
        using var directory = TemporaryDirectory.Create();
        var repository = NewRepository(directory.Path);
        var transaction = NewTransaction(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero),
            merchant: "REDACTED STORE",
            category: "Groceries",
            amount: 125.75m);

        await repository.SaveAsync(transaction);

        var file = Assert.ContainsSingle(Directory.GetFiles(directory.Path));
        var contents = await File.ReadAllTextAsync(file);

        Assert.DoesNotContain("REDACTED STORE", contents);
        Assert.DoesNotContain("Groceries", contents);
        Assert.DoesNotContain("125.75", contents);
    }

    private static EncryptedTransactionRepository NewRepository(string directory)
    {
        return new EncryptedTransactionRepository(NewStore(directory));
    }

    private static FileEncryptedStore NewStore(string directory)
    {
        var key = Enumerable.Repeat(0x42, 32).Select(value => (byte)value).ToArray();
        return new FileEncryptedStore(directory, key);
    }

    private sealed class DelayedInMemoryEncryptedStore : IEncryptedStore
    {
        private readonly object gate = new();
        private List<FinancialTransaction>? transactions;

        public async Task<T?> LoadAsync<T>(
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            List<FinancialTransaction>? snapshot;

            lock (gate)
            {
                snapshot = transactions?.ToList();
            }

            await Task.Delay(5, cancellationToken).ConfigureAwait(false);

            return snapshot is null
                ? default
                : (T)(object)snapshot;
        }

        public async Task SaveAsync<T>(
            string partitionKey,
            T value,
            CancellationToken cancellationToken = default)
        {
            if (value is not List<FinancialTransaction> valueTransactions)
            {
                throw new InvalidOperationException("Only transaction lists are supported by this test store.");
            }

            var snapshot = valueTransactions.ToList();

            await Task.Delay(5, cancellationToken).ConfigureAwait(false);

            lock (gate)
            {
                transactions = snapshot;
            }
        }
    }

    private static FinancialTransaction NewTransaction(
        Guid id,
        DateTimeOffset occurredAt,
        string merchant,
        string category,
        decimal amount)
    {
        return new FinancialTransaction(
            id,
            new MoneyAmount(amount),
            TransactionDirection.Debit,
            TransactionSource.ManualCash,
            occurredAt,
            merchant,
            category,
            sourceReferenceHash: null);
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
                $"hasbemaal-transaction-repository-test-{Guid.NewGuid():N}");

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