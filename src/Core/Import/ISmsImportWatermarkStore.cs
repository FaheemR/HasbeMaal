namespace HasbeMaal.Core.Import;

/// <summary>
/// Persists the incremental SMS import watermark (the newest imported message timestamp) so
/// incremental scans survive across app sessions. Implemented by the persistence layer. Only a
/// timestamp is stored; no message content, sender, or reference ever crosses this boundary.
/// </summary>
public interface ISmsImportWatermarkStore
{
    /// <summary>
    /// Returns the persisted watermark, or <see langword="null"/> when no import has completed yet.
    /// </summary>
    Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <paramref name="watermark"/> as the new incremental watermark. A <see langword="null"/>
    /// value resets scanning to the full inbox on the next import.
    /// </summary>
    Task SetAsync(DateTimeOffset? watermark, CancellationToken cancellationToken = default);
}
