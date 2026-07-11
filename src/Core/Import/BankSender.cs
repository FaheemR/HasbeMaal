namespace HasbeMaal.Core.Import;

/// <summary>
/// A curated bank or UPI app: a stable <see cref="Id"/>, a display <see cref="Name"/>, and the set
/// of official DLT sender header tokens it uses (e.g. <c>HDFCBK</c>). Header tokens are PUBLIC
/// alphanumeric SMS sender identifiers, never phone numbers, UPI ids, or account numbers, so
/// nothing here is user data. Headers are normalized to uppercase alphanumerics and de-duplicated.
/// </summary>
public sealed class BankSender
{
    public BankSender(string id, string name, IReadOnlyList<string> headers)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Bank id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bank name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(headers);

        Id = id.Trim();
        Name = name.Trim();

        var normalized = new List<string>(headers.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var header in headers)
        {
            var token = BankSenderMatcher.NormalizeHeader(header);
            if (token.Length > 0 && seen.Add(token))
            {
                normalized.Add(token);
            }
        }

        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one header is required.", nameof(headers));
        }

        Headers = normalized;
    }

    /// <summary>Stable, lowercase identifier used to persist a user's bank selection.</summary>
    public string Id { get; }

    /// <summary>Display name shown in the bank-selection UI.</summary>
    public string Name { get; }

    /// <summary>Normalized, de-duplicated official DLT sender header tokens for this sender.</summary>
    public IReadOnlyList<string> Headers { get; }
}
