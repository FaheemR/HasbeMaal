using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Import;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class SmsTransactionImporterTests
{
    // Synthetic, redacted SMS bodies only. No real senders, UPI ids, or account numbers.
    private const string HighDebitWithRef = "Paid Rs. 245.50 to REDACTED STORE via UPI. UPI Ref SYNTH001.";
    private const string HighCreditWithRef = "Received INR 850.00 from REDACTED STORE via UPI. UPI Ref SYNTH002.";
    private const string HighDebitNoRef = "Paid Rs. 250.00 to REDACTED CAFE via UPI.";
    private const string MediumDebitNoRef = "Rs. 250.00 debited via UPI.";
    private const string NonFinancial = "Your appointment is confirmed for tomorrow.";

    private static readonly DateTimeOffset BaseInstant = new(2026, 7, 9, 10, 30, 0, TimeSpan.Zero);

    [TestMethod]
    public void Constructor_NullParser_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new SmsTransactionImporter(null!, new RecordingTransactionRepository()));
    }

    [TestMethod]
    public void Constructor_NullRepository_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new SmsTransactionImporter(new DeterministicSmsTransactionParser(), null!));
    }

    [TestMethod]
    public async Task ImportAsync_NullMessages_Throws()
    {
        var importer = NewImporter(out _);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await importer.ImportAsync(null!));
    }

    [TestMethod]
    public async Task ImportAsync_EmptyBatch_ReturnsEmptyResultAndLeavesWatermarkUnchanged()
    {
        var importer = NewImporter(out var repository);
        var watermark = BaseInstant;

        var result = await importer.ImportAsync([], watermark);

        Assert.IsEmpty(result.Ready);
        Assert.IsEmpty(result.NeedsReview);
        Assert.AreEqual(0, result.DuplicateCount);
        Assert.AreEqual(0, result.IgnoredCount);
        Assert.AreEqual(watermark, result.Watermark);
        Assert.AreEqual(0, repository.SaveManyCallCount);
    }

    [TestMethod]
    public async Task ImportAsync_HighConfidenceUnique_IsReadyAndSaved()
    {
        var importer = NewImporter(out var repository);

        var result = await importer.ImportAsync([new SmsInboxMessage(HighDebitWithRef, BaseInstant)]);

        var ready = Assert.ContainsSingle(result.Ready);
        Assert.AreEqual(245.50m, ready.Amount.Amount);
        Assert.AreEqual(TransactionDirection.Debit, ready.Direction);
        Assert.IsEmpty(result.NeedsReview);
        Assert.AreEqual(1, repository.SaveManyCallCount);
        var saved = Assert.ContainsSingle(repository.LastSaveManyBatch);
        Assert.AreSame(ready, saved);
    }

    [TestMethod]
    public async Task ImportAsync_MediumConfidenceUnique_IsNeedsReviewAndNotSaved()
    {
        var importer = NewImporter(out var repository);

        var result = await importer.ImportAsync([new SmsInboxMessage(MediumDebitNoRef, BaseInstant)]);

        var candidate = Assert.ContainsSingle(result.NeedsReview);
        Assert.AreEqual(ParseConfidence.Medium, candidate.Confidence);
        Assert.AreEqual(250.00m, candidate.Transaction.Amount.Amount);
        Assert.IsEmpty(result.Ready);
        Assert.AreEqual(0, repository.SaveManyCallCount);
        Assert.IsEmpty(repository.SavedTransactions);
    }

    [TestMethod]
    public async Task ImportAsync_NonFinancialBody_IsIgnored()
    {
        var importer = NewImporter(out var repository);

        var result = await importer.ImportAsync([new SmsInboxMessage(NonFinancial, BaseInstant)]);

        Assert.AreEqual(1, result.IgnoredCount);
        Assert.IsEmpty(result.Ready);
        Assert.IsEmpty(result.NeedsReview);
        Assert.AreEqual(0, result.DuplicateCount);
        Assert.AreEqual(0, repository.SaveManyCallCount);
    }

    [TestMethod]
    public async Task ImportAsync_CommittedTransactionOccurredAtEqualsReceivedAt()
    {
        var importer = NewImporter(out _);
        var receivedAt = new DateTimeOffset(2026, 7, 9, 14, 45, 12, TimeSpan.FromHours(5));

        var result = await importer.ImportAsync([new SmsInboxMessage(HighDebitWithRef, receivedAt)]);

        var ready = Assert.ContainsSingle(result.Ready);
        Assert.AreEqual(receivedAt, ready.OccurredAt);
    }

    [TestMethod]
    public async Task ImportAsync_ReimportingSameBatch_ClassifiesAsDuplicateAndSavesNothing()
    {
        var importer = NewImporter(out var repository);
        SmsInboxMessage[] batch =
        [
            new(HighDebitWithRef, BaseInstant),
            new(HighCreditWithRef, BaseInstant.AddMinutes(1)),
        ];

        var first = await importer.ImportAsync(batch);
        Assert.HasCount(2, first.Ready);
        Assert.AreEqual(1, repository.SaveManyCallCount);

        var second = await importer.ImportAsync(batch);

        Assert.IsEmpty(second.Ready);
        Assert.IsEmpty(second.NeedsReview);
        Assert.AreEqual(2, second.DuplicateCount);
        // SaveManyAsync must not be called again because there is nothing ready to commit.
        Assert.AreEqual(1, repository.SaveManyCallCount);
    }

    [TestMethod]
    public async Task ImportAsync_ReferenceLessDuplicateWithinBatch_SecondIsDuplicate()
    {
        var importer = NewImporter(out _);
        SmsInboxMessage[] batch =
        [
            new(HighDebitNoRef, BaseInstant),
            // Same amount, direction, minute, and merchant, no reference -> composite duplicate.
            new(HighDebitNoRef, BaseInstant.AddSeconds(30)),
        ];

        var result = await importer.ImportAsync(batch);

        Assert.HasCount(1, result.Ready);
        Assert.AreEqual(1, result.DuplicateCount);
    }

    [TestMethod]
    public async Task ImportAsync_ReferenceLessCompositeKey_DoesNotOverMatchOnDifferentMinute()
    {
        var importer = NewImporter(out _);
        SmsInboxMessage[] batch =
        [
            new(HighDebitNoRef, BaseInstant),
            // Same merchant/amount/direction but a different minute -> distinct events.
            new(HighDebitNoRef, BaseInstant.AddMinutes(1)),
        ];

        var result = await importer.ImportAsync(batch);

        Assert.HasCount(2, result.Ready);
        Assert.AreEqual(0, result.DuplicateCount);
    }

    [TestMethod]
    public async Task ImportAsync_WatermarkAdvancesToNewestReceivedAt()
    {
        var importer = NewImporter(out _);
        var newest = BaseInstant.AddMinutes(5);
        SmsInboxMessage[] batch =
        [
            new(HighDebitWithRef, BaseInstant),
            new(HighCreditWithRef, newest),
        ];

        var result = await importer.ImportAsync(batch);

        Assert.AreEqual(newest, result.Watermark);
    }

    [TestMethod]
    public async Task ImportAsync_MessagesAtOrBelowWatermark_AreSkippedAsDuplicates()
    {
        var importer = NewImporter(out _);
        var watermark = BaseInstant;
        SmsInboxMessage[] batch =
        [
            // At the watermark -> skipped.
            new(HighDebitWithRef, watermark),
            // After the watermark -> processed.
            new(HighCreditWithRef, watermark.AddMinutes(1)),
        ];

        var result = await importer.ImportAsync(batch, watermark);

        Assert.AreEqual(1, result.DuplicateCount);
        var ready = Assert.ContainsSingle(result.Ready);
        Assert.AreEqual(TransactionDirection.Credit, ready.Direction);
        Assert.AreEqual(watermark.AddMinutes(1), result.Watermark);
    }

    [TestMethod]
    public async Task ImportAsync_LoadsExistingTransactionsExactlyOnce()
    {
        var importer = NewImporter(out var repository);
        SmsInboxMessage[] batch =
        [
            new(HighDebitWithRef, BaseInstant),
            new(HighCreditWithRef, BaseInstant.AddMinutes(1)),
            new(MediumDebitNoRef, BaseInstant.AddMinutes(2)),
        ];

        await importer.ImportAsync(batch);

        Assert.AreEqual(1, repository.ListCallCount);
    }

    private static SmsTransactionImporter NewImporter(out RecordingTransactionRepository repository)
    {
        repository = new RecordingTransactionRepository();
        return new SmsTransactionImporter(new DeterministicSmsTransactionParser(), repository);
    }

    private sealed class RecordingTransactionRepository : ITransactionRepository
    {
        private readonly List<FinancialTransaction> transactions;

        public RecordingTransactionRepository(IEnumerable<FinancialTransaction>? seed = null)
        {
            transactions = seed?.ToList() ?? [];
        }

        public int ListCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public int SaveManyCallCount { get; private set; }

        public List<FinancialTransaction> SavedTransactions { get; } = [];

        public List<FinancialTransaction> LastSaveManyBatch { get; private set; } = [];

        public Task SaveAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            SaveCallCount++;
            Upsert(transaction);
            SavedTransactions.Add(transaction);

            return Task.CompletedTask;
        }

        public Task SaveManyAsync(
            IReadOnlyList<FinancialTransaction> transactionsToSave,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transactionsToSave);

            SaveManyCallCount++;
            LastSaveManyBatch = transactionsToSave.ToList();

            foreach (var transaction in transactionsToSave)
            {
                Upsert(transaction);
                SavedTransactions.Add(transaction);
            }

            return Task.CompletedTask;
        }

        public Task<FinancialTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(transactions.SingleOrDefault(transaction => transaction.Id == id));
        }

        public Task<IReadOnlyList<FinancialTransaction>> ListAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            ListCallCount++;

            IReadOnlyList<FinancialTransaction> result = transactions
                .Where(transaction =>
                {
                    var date = DateOnly.FromDateTime(transaction.OccurredAt.UtcDateTime);

                    return date >= from && date <= to;
                })
                .ToList();

            return Task.FromResult(result);
        }

        private void Upsert(FinancialTransaction transaction)
        {
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
}
