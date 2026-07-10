using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Core.Import;

/// <summary>
/// Deterministically turns a batch of <see cref="SmsInboxMessage"/> into persisted transactions
/// and review candidates. The algorithm is O(n) in the batch size: existing transactions are
/// loaded exactly once and duplicate detection uses in-memory hash sets. Only high-confidence,
/// unique transactions are auto-committed; lower-confidence candidates are returned for review
/// and are not persisted by this importer. Raw message bodies, senders, and references are never
/// logged or stored.
/// </summary>
public sealed class SmsTransactionImporter : ISmsTransactionImporter
{
    private readonly ISmsTransactionParser parser;
    private readonly ITransactionRepository repository;

    public SmsTransactionImporter(ISmsTransactionParser parser, ITransactionRepository repository)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(repository);

        this.parser = parser;
        this.repository = repository;
    }

    /// <summary>
    /// Imports a batch of messages.
    /// </summary>
    /// <param name="messages">The messages to import. Required.</param>
    /// <param name="watermark">
    /// When provided, messages received at or before this instant are treated as already-seen and
    /// skipped as duplicates.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<SmsImportResult> ImportAsync(
        IReadOnlyList<SmsInboxMessage> messages,
        DateTimeOffset? watermark = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var existing = await repository
            .ListAsync(DateOnly.MinValue, DateOnly.MaxValue, cancellationToken)
            .ConfigureAwait(false);

        var existingHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingKeys = new HashSet<TransactionCompositeKey>();
        foreach (var transaction in existing)
        {
            if (transaction.SourceReferenceHash is not null)
            {
                existingHashes.Add(transaction.SourceReferenceHash);
            }

            existingKeys.Add(TransactionCompositeKey.FromTransaction(transaction));
        }

        var batchHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchKeys = new HashSet<TransactionCompositeKey>();

        var ready = new List<FinancialTransaction>();
        var needsReview = new List<SmsImportReviewCandidate>();
        var duplicateCount = 0;
        var ignoredCount = 0;
        var newWatermark = watermark;

        // Stable ascending order gives deterministic first-wins de-duplication within the batch.
        foreach (var message in messages.OrderBy(message => message.ReceivedAt))
        {
            newWatermark = newWatermark is null
                ? message.ReceivedAt
                : (message.ReceivedAt > newWatermark ? message.ReceivedAt : newWatermark);

            if (watermark.HasValue && message.ReceivedAt <= watermark.Value)
            {
                duplicateCount++;
                continue;
            }

            var parsed = parser.TryParse(message.Body);
            if (parsed is null)
            {
                ignoredCount++;
                continue;
            }

            parsed = parsed with { OccurredAt = message.ReceivedAt };
            var transaction = FinancialTransactionFactory.Create(parsed);

            if (transaction.SourceReferenceHash is not null)
            {
                // Source reference hash is the authoritative identity when present.
                if (existingHashes.Contains(transaction.SourceReferenceHash) ||
                    !batchHashes.Add(transaction.SourceReferenceHash))
                {
                    duplicateCount++;
                    continue;
                }
            }
            else
            {
                // Fall back to a deterministic composite key for reference-less messages.
                var key = TransactionCompositeKey.FromTransaction(transaction);
                if (existingKeys.Contains(key) || !batchKeys.Add(key))
                {
                    duplicateCount++;
                    continue;
                }
            }

            if (parsed.Confidence == ParseConfidence.High)
            {
                ready.Add(transaction);
            }
            else
            {
                needsReview.Add(new SmsImportReviewCandidate(transaction, parsed.Confidence));
            }
        }

        if (ready.Count > 0)
        {
            await repository.SaveManyAsync(ready, cancellationToken).ConfigureAwait(false);
        }

        return new SmsImportResult(ready, needsReview, duplicateCount, ignoredCount, newWatermark);
    }
}
