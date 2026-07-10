using System.Text;

namespace HasbeMaal.Presentation.ViewModels;

/// <summary>
/// Deterministic, platform-agnostic allowlist of SMS sender header codes used to decide
/// whether a message is a candidate transaction source. The seeded codes are PUBLIC
/// alphanumeric sender header IDs (e.g. bank and UPI SMS headers), not user data: no phone
/// number, UPI id, or account number is ever stored here. Matching is fully deterministic
/// and uses ordinal/invariant operations only.
/// </summary>
/// <remarks>
/// The sender address is only ever used for this allowlist check on the platform side. It is
/// dropped before any message crosses the Core boundary and is never logged or persisted.
/// </remarks>
public sealed class SmsSenderAllowlist
{
    private const int MinimumCodeLength = 3;

    // Public India bank / UPI SMS sender header codes. Each is at least five characters to
    // avoid false substring matches. These are not PII; do not add phone numbers, UPI ids,
    // or account numbers here.
    private static readonly string[] DefaultCodes =
    [
        "HDFCBK", "ICICIB", "SBIINB", "SBICRD", "AXISBK", "KOTAKB", "YESBNK",
        "PNBSMS", "BOBTXN", "CANBNK", "IDFCFB", "INDUSB", "RBLBNK", "UNIONB",
        "CENTBK", "FEDBNK", "CITIBK", "HSBCIN", "SCBANK", "AMEXIN",
        "PAYTM", "PHONPE", "BHIMUP", "MOBIKW", "AMZNPY"
    ];

    private readonly HashSet<string> _codes;

    /// <summary>Creates an allowlist seeded with the built-in default codes.</summary>
    public SmsSenderAllowlist()
        : this(Enumerable.Empty<string>())
    {
    }

    /// <summary>
    /// Creates an allowlist that merges <paramref name="additionalCodes"/> with the built-in
    /// defaults. Forward-compatible with a user-managed allowlist in a later slice.
    /// </summary>
    public SmsSenderAllowlist(IEnumerable<string> additionalCodes)
    {
        ArgumentNullException.ThrowIfNull(additionalCodes);

        _codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in DefaultCodes.Concat(additionalCodes))
        {
            var normalized = Normalize(code);
            if (normalized.Length >= MinimumCodeLength)
            {
                _codes.Add(normalized);
            }
        }
    }

    /// <summary>The normalized seeded codes. Exposed for tests only.</summary>
    public IReadOnlyCollection<string> Codes => _codes;

    /// <summary>
    /// Returns true when the sender address matches any seeded code. The address is
    /// normalized (uppercased, non-alphanumeric characters removed) so operator prefixes
    /// such as <c>AX-HDFCBK</c> still match <c>HDFCBK</c>. Returns false for null or
    /// whitespace input.
    /// </summary>
    public bool IsAllowed(string? senderAddress)
    {
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            return false;
        }

        var normalized = Normalize(senderAddress);
        if (normalized.Length == 0)
        {
            return false;
        }

        foreach (var code in _codes)
        {
            if (normalized.Contains(code, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (IsAsciiAlphanumeric(ch))
            {
                builder.Append(ToUpperAscii(ch));
            }
        }

        return builder.ToString();
    }

    private static bool IsAsciiAlphanumeric(char ch) =>
        (ch >= '0' && ch <= '9') ||
        (ch >= 'A' && ch <= 'Z') ||
        (ch >= 'a' && ch <= 'z');

    private static char ToUpperAscii(char ch) =>
        ch is >= 'a' and <= 'z' ? (char)(ch - 32) : ch;
}
