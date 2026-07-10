namespace HasbeMaal.Core.Import;

/// <summary>
/// Turns a batch of <see cref="SmsInboxMessage"/> into an <see cref="SmsImportResult"/>. Only
/// high-confidence, unique transactions are auto-committed; lower-confidence candidates are
/// returned for review and are not persisted by the importer. Defined so callers (for example the
/// presentation layer) can depend on the contract without taking a dependency on the parser or
/// repository the concrete implementation uses.
/// </summary>
public interface ISmsTransactionImporter
{
    /// <summary>
    /// Imports a batch of messages.
    /// </summary>
    /// <param name="messages">The messages to import. Required.</param>
    /// <param name="watermark">
    /// When provided, messages received at or before this instant are treated as already-seen and
    /// skipped as duplicates.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SmsImportResult> ImportAsync(
        IReadOnlyList<SmsInboxMessage> messages,
        DateTimeOffset? watermark = null,
        CancellationToken cancellationToken = default);
}
