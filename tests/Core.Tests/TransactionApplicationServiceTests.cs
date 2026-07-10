using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class TransactionApplicationServiceTests
{
    [TestMethod]
    public void Constructor_NullRepository_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TransactionApplicationService(null!));
    }

    [TestMethod]
    public async Task SaveAsync_ValidTransaction_PersistsAndReturnsSaved()
    {
        var repository = new FakeTransactionRepository();
        var service = new TransactionApplicationService(repository);
        var transaction = NewTransaction(Guid.NewGuid(), "HASH001");

        var result = await service.SaveAsync(transaction);

        Assert.AreEqual(TransactionSaveStatus.Saved, result.Status);
        Assert.AreSame(transaction, result.Transaction);
        Assert.AreEqual(1, repository.SaveCallCount);
        Assert.AreSame(transaction, repository.SavedTransactions.Single());
        Assert.AreEqual(DateOnly.MinValue, repository.ListCalls.Single().From);
        Assert.AreEqual(DateOnly.MaxValue, repository.ListCalls.Single().To);
    }

    [TestMethod]
    public async Task SaveAsync_NullTransaction_Throws()
    {
        var service = new TransactionApplicationService(new FakeTransactionRepository());

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await service.SaveAsync(null!));
    }

    [TestMethod]
    public async Task SaveAsync_DuplicateSourceReferenceHash_IgnoresWithoutSaving()
    {
        var existing = NewTransaction(Guid.NewGuid(), "HASH002");
        var repository = new FakeTransactionRepository([existing]);
        var service = new TransactionApplicationService(repository);
        var duplicate = NewTransaction(Guid.NewGuid(), "hash002");

        var result = await service.SaveAsync(duplicate);

        Assert.AreEqual(TransactionSaveStatus.DuplicateIgnored, result.Status);
        Assert.AreSame(duplicate, result.Transaction);
        Assert.AreEqual(0, repository.SaveCallCount);
        Assert.IsEmpty(repository.SavedTransactions);
    }

    [TestMethod]
    public async Task SaveAsync_SameIdWithSameSourceReferenceHash_Persists()
    {
        var id = Guid.NewGuid();
        var existing = NewTransaction(id, "HASH003");
        var repository = new FakeTransactionRepository([existing]);
        var service = new TransactionApplicationService(repository);
        var transaction = NewTransaction(id, "hash003");

        var result = await service.SaveAsync(transaction);

        Assert.AreEqual(TransactionSaveStatus.Saved, result.Status);
        Assert.AreEqual(1, repository.SaveCallCount);
        Assert.AreSame(transaction, repository.SavedTransactions.Single());
    }

    [TestMethod]
    public async Task SaveAsync_ConcurrentSameSourceReferenceHashWithDifferentIds_SavesOnlyOne()
    {
        var repository = new FakeTransactionRepository();
        repository.PauseSaves();
        var service = new TransactionApplicationService(repository);
        var first = NewTransaction(Guid.NewGuid(), "HASH004");
        var second = NewTransaction(Guid.NewGuid(), "HASH004");

        var firstSave = service.SaveAsync(first);
        await repository.WaitForFirstSaveAttemptAsync().WaitAsync(TimeSpan.FromSeconds(1));
        var secondSave = service.SaveAsync(second);

        repository.AllowPausedSavesToComplete();

        var results = await Task.WhenAll(firstSave, secondSave);
        var savedResults = results
            .Where(result => result.Status == TransactionSaveStatus.Saved)
            .ToArray();
        var duplicateIgnoredResults = results
            .Where(result => result.Status == TransactionSaveStatus.DuplicateIgnored)
            .ToArray();

        Assert.HasCount(1, savedResults);
        Assert.HasCount(1, duplicateIgnoredResults);
        Assert.AreNotEqual(savedResults.Single().Transaction.Id, duplicateIgnoredResults.Single().Transaction.Id);
        Assert.AreEqual(1, repository.SaveCallCount);
        Assert.HasCount(1, repository.SavedTransactions);
        Assert.AreSame(savedResults.Single().Transaction, repository.SavedTransactions.Single());
    }

    [TestMethod]
    public async Task SaveManyAsync_NullTransactions_Throws()
    {
        var service = new TransactionApplicationService(new FakeTransactionRepository());

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await service.SaveManyAsync(null!));
    }

    [TestMethod]
    public async Task SaveManyAsync_EmptyList_DoesNotTouchRepository()
    {
        var repository = new FakeTransactionRepository();
        var service = new TransactionApplicationService(repository);

        var results = await service.SaveManyAsync([]);

        Assert.IsEmpty(results);
        Assert.AreEqual(0, repository.ListCallCount);
        Assert.IsEmpty(repository.SavedTransactions);
    }

    [TestMethod]
    public async Task SaveManyAsync_UniqueTransactions_LoadsExistingOnceAndPersistsAll()
    {
        var repository = new FakeTransactionRepository();
        var service = new TransactionApplicationService(repository);
        var first = NewTransaction(Guid.NewGuid(), "HASH020");
        var second = NewTransaction(Guid.NewGuid(), "HASH021");

        var results = await service.SaveManyAsync([first, second]);

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(result => result.Status == TransactionSaveStatus.Saved));
        Assert.AreEqual(1, repository.ListCallCount);
        Assert.HasCount(2, repository.SavedTransactions);
    }

    [TestMethod]
    public async Task SaveManyAsync_DuplicateOfExisting_IgnoresThatItemAndSavesRest()
    {
        var existing = NewTransaction(Guid.NewGuid(), "HASH022");
        var repository = new FakeTransactionRepository([existing]);
        var service = new TransactionApplicationService(repository);
        var duplicate = NewTransaction(Guid.NewGuid(), "hash022");
        var unique = NewTransaction(Guid.NewGuid(), "HASH023");

        var results = await service.SaveManyAsync([duplicate, unique]);

        Assert.AreEqual(TransactionSaveStatus.DuplicateIgnored, results[0].Status);
        Assert.AreEqual(TransactionSaveStatus.Saved, results[1].Status);
        Assert.AreSame(unique, repository.SavedTransactions.Single());
    }

    [TestMethod]
    public async Task SaveManyAsync_DuplicateSourceReferenceHashWithinBatch_SavesOnlyFirst()
    {
        var repository = new FakeTransactionRepository();
        var service = new TransactionApplicationService(repository);
        var first = NewTransaction(Guid.NewGuid(), "HASH024");
        var second = NewTransaction(Guid.NewGuid(), "hash024");

        var results = await service.SaveManyAsync([first, second]);

        Assert.AreEqual(TransactionSaveStatus.Saved, results[0].Status);
        Assert.AreEqual(TransactionSaveStatus.DuplicateIgnored, results[1].Status);
        Assert.AreSame(first, repository.SavedTransactions.Single());
    }

    [TestMethod]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var transaction = NewTransaction(Guid.NewGuid(), "HASH005");
        var repository = new FakeTransactionRepository([transaction]);
        var service = new TransactionApplicationService(repository);

        var result = await service.GetByIdAsync(transaction.Id);

        Assert.AreSame(transaction, result);
        Assert.AreEqual(1, repository.GetByIdCallCount);
        Assert.AreEqual(transaction.Id, repository.LastGetById);
    }

    [TestMethod]
    public async Task ListAsync_DelegatesRangeAndReturnsRepositoryResults()
    {
        var included = NewTransaction(Guid.NewGuid(), "HASH006", new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero));
        var outside = NewTransaction(Guid.NewGuid(), "HASH007", new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeTransactionRepository([included, outside]);
        var service = new TransactionApplicationService(repository);
        var from = new DateOnly(2026, 7, 1);
        var to = new DateOnly(2026, 7, 31);

        var result = await service.ListAsync(from, to);

        Assert.AreEqual(1, repository.ListCallCount);
        Assert.AreEqual(from, repository.ListCalls.Single().From);
        Assert.AreEqual(to, repository.ListCalls.Single().To);
        Assert.HasCount(1, result);
        Assert.AreSame(included, result.Single());
    }

    [TestMethod]
    public async Task DeleteManyAsync_RemovesTransactionsThroughRepository()
    {
        var first = NewTransaction(Guid.NewGuid(), "HASH010");
        var second = NewTransaction(Guid.NewGuid(), "HASH011");
        var repository = new FakeTransactionRepository([first, second]);
        var service = new TransactionApplicationService(repository);

        await service.DeleteManyAsync([first.Id]);

        Assert.AreEqual(first.Id, repository.DeletedIds.Single());
        Assert.IsNull(await repository.GetByIdAsync(first.Id));
        Assert.AreEqual(second.Id, (await repository.GetByIdAsync(second.Id))?.Id);
    }

    [TestMethod]
    public async Task DeleteManyAsync_EmptyIds_DoesNotTouchRepository()
    {
        var repository = new FakeTransactionRepository();
        var service = new TransactionApplicationService(repository);

        await service.DeleteManyAsync([]);

        Assert.IsEmpty(repository.DeletedIds);
    }

    [TestMethod]
    public async Task DeleteManyAsync_NullIds_Throws()
    {
        var service = new TransactionApplicationService(new FakeTransactionRepository());

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await service.DeleteManyAsync(null!));
    }

    private static FinancialTransaction NewTransaction(
        Guid id,
        string? sourceReferenceHash,
        DateTimeOffset? occurredAt = null) => new(
        id,
        new MoneyAmount(100m),
        TransactionDirection.Debit,
        TransactionSource.UpiSms,
        occurredAt ?? new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
        "REDACTED STORE",
        "Groceries",
        sourceReferenceHash);

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly object syncRoot = new();
        private readonly List<FinancialTransaction> transactions;
        private TaskCompletionSource? firstSaveStarted;
        private TaskCompletionSource? pausedSavesMayComplete;

        public FakeTransactionRepository(IEnumerable<FinancialTransaction>? transactions = null)
        {
            this.transactions = transactions?.ToList() ?? [];
        }

        public int SaveCallCount { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public int ListCallCount { get; private set; }

        public Guid? LastGetById { get; private set; }

        public List<FinancialTransaction> SavedTransactions { get; } = [];

        public List<(DateOnly From, DateOnly To)> ListCalls { get; } = [];

        public void PauseSaves()
        {
            firstSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            pausedSavesMayComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task WaitForFirstSaveAttemptAsync() =>
            firstSaveStarted?.Task ?? Task.CompletedTask;

        public void AllowPausedSavesToComplete() =>
            pausedSavesMayComplete?.TrySetResult();

        public async Task SaveAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
        {
            Task? pausedSavesMayCompleteTask;

            lock (syncRoot)
            {
                SaveCallCount++;
                firstSaveStarted?.TrySetResult();
                pausedSavesMayCompleteTask = pausedSavesMayComplete?.Task;
            }

            if (pausedSavesMayCompleteTask is not null)
            {
                await pausedSavesMayCompleteTask.WaitAsync(cancellationToken);
            }

            lock (syncRoot)
            {
                SavedTransactions.Add(transaction);

                var existingIndex = transactions.FindIndex(existing => existing.Id == transaction.Id);
                if (existingIndex >= 0)
                {
                    transactions[existingIndex] = transaction;
                }
                else
                {
                    transactions.Add(transaction);
                }
            }
        }

        public Task SaveManyAsync(
            IReadOnlyList<FinancialTransaction> transactionsToSave,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transactionsToSave);

            lock (syncRoot)
            {
                foreach (var transaction in transactionsToSave)
                {
                    SavedTransactions.Add(transaction);

                    var existingIndex = transactions.FindIndex(existing => existing.Id == transaction.Id);
                    if (existingIndex >= 0)
                    {
                        transactions[existingIndex] = transaction;
                    }
                    else
                    {
                        transactions.Add(transaction);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public List<Guid> DeletedIds { get; } = [];

        public Task DeleteManyAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ids);

            lock (syncRoot)
            {
                DeletedIds.AddRange(ids);
                var targetIds = new HashSet<Guid>(ids);
                transactions.RemoveAll(transaction => targetIds.Contains(transaction.Id));
            }

            return Task.CompletedTask;
        }

        public Task<FinancialTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
            {
                GetByIdCallCount++;
                LastGetById = id;

                return Task.FromResult(transactions.SingleOrDefault(transaction => transaction.Id == id));
            }
        }

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
            {
                ListCallCount++;
                ListCalls.Add((from, to));

                IReadOnlyList<FinancialTransaction> result = transactions
                    .Where(transaction =>
                    {
                        var date = DateOnly.FromDateTime(transaction.OccurredAt.UtcDateTime);

                        return date >= from && date <= to;
                    })
                    .ToList();

                return Task.FromResult(result);
            }
        }
    }
}