using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Parsing;

public sealed class DeterministicSmsTransactionParser : ISmsTransactionParser
{
    private static readonly Regex AmountRegex = new(
        @"\b(?:Rs\.?|INR)\s*(?<amount>[0-9,]+(?:\.[0-9]{1,2})?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // "to/at/from <merchant>" but never an account phrase (to your card / to a/c ...).
    private static readonly Regex MerchantRegex = new(
        @"\b(?:to|at|from)\s+(?!your\b|you\b|the\b|a/?c\b|account\b|card\b)(?<merchant>[A-Za-z0-9&.\-' ]+?)(?:\s+(?:via|on|using|ref|upi|a/?c|account|card|dated|worth|of|for|is)\b|[.,;:]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ReferenceRegex = new(
        @"\b(?:ref(?:erence)?(?:\s+no)?|upi\s+ref(?:\s+no)?|txn)\s*[:#]?\s*(?<reference>[A-Z0-9-]{6,})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // "on 10-JUL-26", "on 10/07/2026", "dated 10-07-26".
    private static readonly Regex DateRegex = new(
        @"\b(?:on|dated)\s+(?<day>\d{1,2})[-/\s](?<month>[A-Za-z]{3,9}|\d{1,2})[-/\s](?<year>\d{2,4})\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // "<bank> Credit/Debit Card XX5005", "Card ending 5005", "a/c XX5005".
    private static readonly Regex AccountRegex = new(
        @"(?<label>(?:[A-Za-z&.]+\s+){0,3}?(?:credit|debit)\s+card|card|a/?c|account)\s+(?:(?:no\.?|number|ending)\s*[:#]?\s*|[xX*·]{1,}\s*)(?<tail>\d{4})(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly string[] MerchantNoiseSuffixes =
    [
        " IN E COMMERCE", " IN E COMMERC", " E COMMERCE", " E COMMERC",
        " PRIVATE LIMITED", " PVT. LTD.", " PVT LTD.", " PVT LTD", " LIMITED", " LTD.", " LTD"
    ];

    private static readonly string[] LeadingMerchantStops =
    [
        "refund", "credited", "credit", "debited", "debit", "payment", "spent",
        "purchase", "received", "paid", "sent", "withdrawn", "transaction", "txn"
    ];

    private static readonly string[] AccountLeadingStops = ["to", "your", "the", "a", "in", "on", "of", "for", "my"];

    public ParsedTransaction? TryParse(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var amountMatch = AmountRegex.Match(message);
        var direction = DetectDirection(message);

        if (!amountMatch.Success || direction is null)
        {
            return null;
        }

        if (!decimal.TryParse(
                amountMatch.Groups["amount"].Value.Replace(",", string.Empty, StringComparison.Ordinal),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        var merchant = ExtractMerchant(message);
        var reference = ExtractReference(message);
        var account = ExtractAccount(message);
        var occurredOn = ExtractDate(message);
        var confidence = merchant == "Unknown" ? ParseConfidence.Medium : ParseConfidence.High;

        return new ParsedTransaction(
            new MoneyAmount(amount),
            merchant,
            OccurredAt: null,
            direction.Value,
            DetectSource(message),
            reference,
            confidence,
            occurredOn,
            account);
    }

    private static TransactionDirection? DetectDirection(string message)
    {
        var normalized = message.ToLowerInvariant();

        if (ContainsAny(normalized, "credited", "received", "refund", "cashback"))
        {
            return TransactionDirection.Credit;
        }

        if (ContainsAny(normalized, "debited", "paid", "spent", "sent", "withdrawn", "purchase"))
        {
            return TransactionDirection.Debit;
        }

        return null;
    }

    private static TransactionSource DetectSource(string message)
    {
        var normalized = message.ToLowerInvariant();

        if (ContainsAny(normalized, "gpay", "google pay", "upi"))
        {
            return TransactionSource.UpiSms;
        }

        if (ContainsAny(normalized, "credit card", "cc "))
        {
            return TransactionSource.CreditCardSms;
        }

        if (normalized.Contains("wallet", StringComparison.Ordinal))
        {
            return TransactionSource.WalletSms;
        }

        return TransactionSource.BankSms;
    }

    private static string ExtractMerchant(string message)
    {
        var merchantMatch = MerchantRegex.Match(message);
        if (merchantMatch.Success)
        {
            var candidate = NormalizeMerchant(merchantMatch.Groups["merchant"].Value);
            if (candidate.Length >= 2)
            {
                return candidate;
            }
        }

        return ExtractLeadingMerchant(message) ?? "Unknown";
    }

    private static string? ExtractLeadingMerchant(string message)
    {
        var boundary = message.Length;

        // The amount region is never part of the merchant; an amount at the very start means the
        // message leads with no merchant text, so leading extraction yields nothing (Unknown).
        var amount = AmountRegex.Match(message);
        if (amount.Success)
        {
            boundary = Math.Min(boundary, amount.Index);
        }

        foreach (var stop in LeadingMerchantStops)
        {
            var index = message.IndexOf(stop, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                boundary = Math.Min(boundary, index);
            }
        }

        if (boundary <= 0)
        {
            return null;
        }

        var head = NormalizeMerchant(message[..boundary]);
        return head.Length >= 2 ? head : null;
    }

    private static string NormalizeMerchant(string raw)
    {
        var value = CollapseWhitespace(raw).Trim().TrimEnd('.', ',', ';', ':').Trim();

        foreach (var suffix in MerchantNoiseSuffixes)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                value = value[..^suffix.Length].Trim();
                break;
            }
        }

        return TitleCase(value);
    }

    private static string? ExtractReference(string message)
    {
        var referenceMatch = ReferenceRegex.Match(message);
        return referenceMatch.Success ? referenceMatch.Groups["reference"].Value : null;
    }

    private static DateOnly? ExtractDate(string message)
    {
        var match = DateRegex.Match(message);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["day"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
        {
            return null;
        }

        var month = ParseMonth(match.Groups["month"].Value);
        if (month is null)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            return null;
        }

        if (year < 100)
        {
            year += 2000;
        }

        if (day < 1 || day > 31 || year < 2000 || year > 2100)
        {
            return null;
        }

        try
        {
            return new DateOnly(year, month.Value, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static int? ParseMonth(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric is >= 1 and <= 12 ? numeric : null;
        }

        var key = value.Length >= 3 ? value[..3].ToUpperInvariant() : value.ToUpperInvariant();
        return key switch
        {
            "JAN" => 1,
            "FEB" => 2,
            "MAR" => 3,
            "APR" => 4,
            "MAY" => 5,
            "JUN" => 6,
            "JUL" => 7,
            "AUG" => 8,
            "SEP" => 9,
            "OCT" => 10,
            "NOV" => 11,
            "DEC" => 12,
            _ => null
        };
    }

    private static string? ExtractAccount(string message)
    {
        var match = AccountRegex.Match(message);
        if (!match.Success)
        {
            return null;
        }

        var tail = match.Groups["tail"].Value;
        var label = CollapseWhitespace(match.Groups["label"].Value).Trim();
        label = StripLeadingWords(label, AccountLeadingStops);

        return label.Length > 0 ? $"{label} ••{tail}" : $"••{tail}";
    }

    private static string StripLeadingWords(string value, string[] stopWords)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var start = 0;
        while (start < words.Length &&
               stopWords.Contains(words[start], StringComparer.OrdinalIgnoreCase))
        {
            start++;
        }

        return string.Join(' ', words[start..]);
    }

    private static string TitleCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            var word = words[i];
            builder.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1)
            {
                builder.Append(word[1..].ToLowerInvariant());
            }
        }

        return builder.ToString();
    }

    private static string CollapseWhitespace(string value) =>
        Regex.Replace(value, @"\s+", " ");

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
    }
}