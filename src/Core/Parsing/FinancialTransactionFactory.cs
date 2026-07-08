using System.Security.Cryptography;
using System.Text;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Parsing;

/// <summary>
/// Maps a <see cref="ParsedTransaction"/> to a persistable <see cref="FinancialTransaction"/>.
/// The raw source <see cref="ParsedTransaction.Reference"/> is never copied onto the domain
/// transaction; instead it is reduced to a deterministic one-way SHA-256 hash so duplicate
/// detection can work without retaining the original reference string.
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
            HashReference(parsed.Reference));
    }

    /// <summary>
    /// Produces a deterministic SHA-256 hash of a source reference.
    /// Normalization is intentionally simple and documented: the reference is trimmed and
    /// upper-cased using the invariant culture before hashing. Returns <c>null</c> when the
    /// reference is null, empty, or whitespace so absent references are not turned into a
    /// hash of an empty string.
    /// </summary>
    /// <remarks>
    /// The raw reference is never returned or stored. Only the hex-encoded digest leaves this
    /// method, preserving privacy while keeping the mapping deterministic.
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
