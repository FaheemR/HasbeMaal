using System.Text;

namespace HasbeMaal.Core.Import;

/// <summary>
/// Deterministic, platform-agnostic matcher that decides whether an SMS sender address belongs
/// to a selected bank or UPI app. Matching is header-exact: the sender is normalized, the operator
/// prefix (<c>XX-</c>) and route suffix (<c>-S</c>/<c>-T</c>/<c>-P</c>/<c>-G</c>) are stripped, and
/// the remaining header token is compared (case-insensitive) against a fixed set of official DLT
/// sender headers.
/// </summary>
/// <remarks>
/// The header set is PUBLIC alphanumeric SMS sender identifiers (e.g. <c>HDFCBK</c>), never phone
/// numbers, UPI ids, or account numbers, so nothing here is user data. The sender address is only
/// ever used for this check on the platform side; it is dropped before any message crosses the
/// Core boundary and is never logged or persisted. All operations are ordinal/invariant.
/// </remarks>
public sealed class BankSenderMatcher
{
    private readonly HashSet<string> headers;

    /// <summary>
    /// Creates a matcher over the supplied header tokens. Tokens are normalized (uppercased,
    /// non-alphanumeric characters removed) before use; empty tokens are ignored.
    /// </summary>
    public BankSenderMatcher(IEnumerable<string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        this.headers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var header in headers)
        {
            var token = NormalizeHeader(header);
            if (token.Length > 0)
            {
                this.headers.Add(token);
            }
        }
    }

    /// <summary>The normalized header tokens this matcher accepts. Exposed for tests only.</summary>
    public IReadOnlyCollection<string> Headers => headers;

    /// <summary>
    /// Creates a matcher scoped to <paramref name="selectedBankIds"/>. A null or empty selection
    /// resolves to the full registry so scanning still works before the user picks banks.
    /// </summary>
    public static BankSenderMatcher ForSelectedBanks(IReadOnlyCollection<string>? selectedBankIds) =>
        new(BankSenderRegistry.ResolveHeaders(selectedBankIds));

    /// <summary>
    /// Returns true when <paramref name="senderAddress"/> resolves to a header in this matcher's
    /// set. Returns false for null, whitespace, or any sender whose header token is not present.
    /// </summary>
    public bool IsMatch(string? senderAddress)
    {
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            return false;
        }

        var token = ExtractHeaderToken(senderAddress);
        return token is not null && headers.Contains(token);
    }

    /// <summary>
    /// Normalizes a curated header value to an uppercase alphanumeric token (dashes and other
    /// separators removed). Returns an empty string for null or all-separator input.
    /// </summary>
    public static string NormalizeHeader(string? header)
    {
        if (string.IsNullOrEmpty(header))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(header.Length);
        foreach (var ch in header)
        {
            if (IsAsciiAlphanumeric(ch))
            {
                builder.Append(ToUpperAscii(ch));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reduces a raw sender address to its header token by keeping only uppercase alphanumerics and
    /// dashes, then stripping a leading two-letter operator prefix and a trailing single-letter
    /// route suffix (S/T/P/G). Returns null when no header token can be isolated.
    /// </summary>
    private static string? ExtractHeaderToken(string senderAddress)
    {
        var cleaned = CleanForSegments(senderAddress);
        if (cleaned.Length == 0)
        {
            return null;
        }

        var segments = cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var start = 0;
        var end = segments.Length;

        // Strip a leading operator prefix (e.g. "AX-") only when another segment remains.
        if (end - start > 1 && IsOperatorPrefix(segments[start]))
        {
            start++;
        }

        // Strip a trailing route suffix (e.g. "-S") only when another segment remains.
        if (end - start > 1 && IsRouteSuffix(segments[end - 1]))
        {
            end--;
        }

        if (end - start <= 0)
        {
            return null;
        }

        // Official DLT headers contain no dashes, so any remaining segments form a single token.
        return end - start == 1 ? segments[start] : string.Concat(segments[start..end]);
    }

    private static string CleanForSegments(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (IsAsciiAlphanumeric(ch))
            {
                builder.Append(ToUpperAscii(ch));
            }
            else if (ch == '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString();
    }

    private static bool IsOperatorPrefix(string segment) =>
        segment.Length == 2 && IsAsciiLetter(segment[0]) && IsAsciiLetter(segment[1]);

    private static bool IsRouteSuffix(string segment) =>
        segment.Length == 1 && segment[0] is 'S' or 'T' or 'P' or 'G';

    private static bool IsAsciiLetter(char ch) =>
        ch is >= 'A' and <= 'Z';

    private static bool IsAsciiAlphanumeric(char ch) =>
        (ch >= '0' && ch <= '9') ||
        (ch >= 'A' && ch <= 'Z') ||
        (ch >= 'a' && ch <= 'z');

    private static char ToUpperAscii(char ch) =>
        ch is >= 'a' and <= 'z' ? (char)(ch - 32) : ch;
}
