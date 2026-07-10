using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Import;

/// <summary>
/// The deterministic outcome of importing a batch of SMS messages.
/// </summary>
/// <param name="Ready">High-confidence, unique transactions that were auto-committed.</param>
/// <param name="NeedsReview">Lower-confidence, unique candidates awaiting user review (not persisted in this slice).</param>
/// <param name="DuplicateCount">Messages skipped because they duplicate an existing or earlier-in-batch transaction, or fall at/under the watermark.</param>
/// <param name="IgnoredCount">Messages that did not parse to a financial transaction.</param>
/// <param name="Watermark">The newest <see cref="SmsInboxMessage.ReceivedAt"/> observed, for the next incremental read.</param>
public sealed record SmsImportResult(
    IReadOnlyList<FinancialTransaction> Ready,
    IReadOnlyList<SmsImportReviewCandidate> NeedsReview,
    int DuplicateCount,
    int IgnoredCount,
    DateTimeOffset? Watermark);
