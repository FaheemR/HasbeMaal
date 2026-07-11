using System.Security.Cryptography;
using System.Text;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Parsing;

/// <summary>
/// Maps a <see cref="ParsedTransaction"/> to a persistable <see cref="FinancialTransaction"/>.
/// The raw source <see cref="ParsedTransaction.Reference"/> is retained on the domain transaction
/// as <see cref="FinancialTransaction.SourceReference"/> so it can be shown to the user; it is
/// stored only inside encrypted persistence, never logged, and removed by delete/purge. The same
/// reference is also reduced to a deterministic one-way SHA-256 hash used for duplicate detection.
/// </summary>
public static class FinancialTransactionFactory
{
    /// <summary>
    /// Creates a <see cref="FinancialTransaction"/> from a parsed transaction.
    /// </summary>
    /// <param name="parsed">The deterministic parse result. Required.</param>
    /// <param name="category">Category to assign. Defaults to <c>"Uncategorized"</c>.</param>
    /// <param name="id">
    /// Optional identity. When <c>null</c> a new <see cref="Guid"/> is generated. Provided for
    /// testability so callers can pin the identity; it does not affect the reference hash.
    /// </param>
    public static FinancialTransaction Create(
        ParsedTransaction parsed,
        string category = "Uncategorized",
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(parsed);

        return new FinancialTransaction(
            id ?? Guid.NewGuid(),
            parsed.Amount,
            parsed.Direction,
            parsed.Source,
            parsed.OccurredAt ?? DateTimeOffset.MinValue,
            parsed.Merchant,
            category,
            HashReference(parsed.Reference),
            parsed.Reference,
            parsed.Account,
            parsed.SourceMessage);
    }

    /// <summary>
    /// Produces a deterministic SHA-256 hash of a source reference.
    /// Normalization is intentionally simple and documented: the reference is trimmed and
    /// upper-cased using the invariant culture before hashing. Returns <c>null</c> when the
    /// reference is null, empty, or whitespace so absent references are not turned into a
    /// hash of an empty string.
    /// </summary>
    /// <remarks>
    /// Only the hex-encoded digest leaves this method. The raw reference is retained separately on
    /// <see cref="FinancialTransaction.SourceReference"/> (encrypted at rest, user-only, never
    /// logged); this hash exists solely so duplicate detection can compare references without
    /// matching on the raw value.
    /// </remarks>
    public static string? HashReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var normalized = reference.Trim().ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
