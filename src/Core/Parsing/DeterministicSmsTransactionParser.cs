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

    // Counterparty extraction, modeled on real bank SMS formats. Debit uses the destination
    // ("to <payee>" / card "at <merchant>"); credit uses the source ("from <payer>"). A VPA target
    // (merchant@bank) is the highest-signal payee handle when present.
    private static readonly Regex MerchantAtRegex = new(
        @"\bat\s+(?!your\b|the\b)(?<m>[A-Za-z0-9@._\-&' ]+?)(?=\s+(?:on|by|avl|not|ref|via|dated|to)\b|[.,;]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex MerchantToRegex = new(
        @"\bto\s+(?!your\b|you\b|the\b|a/?c\b|account\b|card\b|block\b|vpa\b)(?<m>[A-Za-z0-9@._\-&' ]+?)(?=\s+(?:on|ref|upi|via|not|dated|a/?c|account|with|is)\b|[.,;]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex MerchantFromRegex = new(
        @"\bfrom\s+(?:vpa\s+)?(?!your\b|you\b|the\b|a/?c\b|account\b|card\b|vpa\b)(?<m>[A-Za-z0-9@._\-&' ]+?)(?=\s+(?:on|via|upi|by|not|a/?c|account|with|ref|rrn|dated)\b|[.,;(]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // "towards <vpa>", "to <vpa>", "from VPA <vpa>", "at <vpa> by UPI".
    private static readonly Regex VpaTargetRegex = new(
        @"\b(?:towards|to|from|at)\s+(?:vpa\s+)?(?<m>[A-Za-z0-9][\w.\-]*@[A-Za-z][\w.]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex VpaRegex = new(
        @"^[A-Za-z0-9][\w.\-]*@[A-Za-z][\w.]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ReferenceRegex = new(
        @"\b(?:ref(?:erence)?(?:\s+no)?|upi\s+ref(?:\s+no)?|rrn(?:\s+no)?|txn)\s*[:#.]?\s*(?<reference>[A-Z0-9-]{6,})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // "on 10-JUL-26", "on 10/07/2026", "dated 10-07-26".
    private static readonly Regex DateRegex = new(
        @"\b(?:on|dated)\s+(?<day>\d{1,2})[-/\s](?<month>[A-Za-z]{3,9}|\d{1,2})[-/\s](?<year>\d{2,4})\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // "<bank> Credit/Debit Card XX5005", "Card ending 5005", "a/c XX5005".
    private static readonly Regex AccountRegex = new(
        @"(?<label>(?:[A-Za-z&.]+\s+){0,3}?(?:credit|debit)\s+card|card|a/?c|account)\s+(?:(?:no\.?|number|ending)\s*[:#]?\s*|[xX*·]{1,}\s*)(?<tail>\d{4})(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // Masked account token (e.g. "XXXX", "XXXXXXXX2459") and long digit runs are never merchant names.
    private static readonly Regex MaskedAccountTokenRegex = new(
        @"[xX*]{4,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LongDigitRunRegex = new(
        @"\d{5,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        var merchant = ExtractMerchant(message, direction.Value);
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

        // A card transaction with no explicit verb (e.g. "Txn Rs X On Card ... by UPI") is a debit.
        if (normalized.Contains("card", StringComparison.Ordinal) &&
            ContainsAny(normalized, "txn", "transaction"))
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

    private static string ExtractMerchant(string message, TransactionDirection direction)
    {
        // A VPA target (merchant@bank) is the clearest payee handle; prefer it when present.
        var vpa = VpaTargetRegex.Match(message);
        if (vpa.Success)
        {
            var handle = vpa.Groups["m"].Value.Trim();
            if (VpaRegex.IsMatch(handle))
            {
                return handle.ToLowerInvariant();
            }
        }

        // Card purchases name the merchant after "at".
        if (TryExtractMerchant(MerchantAtRegex, message, out var atMerchant))
        {
            return atMerchant;
        }

        // Debit destinations ("to <payee>") vs credit sources ("from <payer>").
        var directional = direction == TransactionDirection.Debit ? MerchantToRegex : MerchantFromRegex;
        if (TryExtractMerchant(directional, message, out var directionalMerchant))
        {
            return directionalMerchant;
        }

        // Fall back to a leading merchant, guarded against account-statement preambles.
        return ExtractLeadingMerchant(message) ?? "Unknown";
    }

    private static bool TryExtractMerchant(Regex pattern, string message, out string merchant)
    {
        merchant = "Unknown";
        var match = pattern.Match(message);
        if (!match.Success)
        {
            return false;
        }

        var candidate = NormalizeMerchant(match.Groups["m"].Value);
        if (!IsLikelyMerchantName(candidate))
        {
            return false;
        }

        merchant = candidate;
        return true;
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
        return IsLikelyMerchantName(head) ? head : null;
    }

    /// <summary>
    /// Rejects leading-text candidates that are not real merchant names: account-statement preambles
    /// ("Your A/c ... has been"), masked account tokens, long digit runs, or fragments without enough
    /// letters. Bank debit SMS often name no payee, so a rejected candidate becomes "Unknown" rather
    /// than surfacing misleading text as the merchant.
    /// </summary>
    private static bool IsLikelyMerchantName(string candidate)
    {
        if (candidate.Length < 2)
        {
            return false;
        }

        var lower = candidate.ToLowerInvariant();
        if (lower.Contains("a/c", StringComparison.Ordinal) ||
            lower.Contains("account", StringComparison.Ordinal) ||
            lower.Contains("has been", StringComparison.Ordinal))
        {
            return false;
        }

        if (MaskedAccountTokenRegex.IsMatch(candidate) || LongDigitRunRegex.IsMatch(candidate))
        {
            return false;
        }

        return candidate.Count(char.IsAsciiLetter) >= 2;
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