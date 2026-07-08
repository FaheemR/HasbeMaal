using System.Globalization;
using System.Text.RegularExpressions;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Parsing;

public sealed class DeterministicSmsTransactionParser : ISmsTransactionParser
{
    private static readonly Regex AmountRegex = new(
        @"\b(?:Rs\.?|INR)\s*(?<amount>[0-9,]+(?:\.[0-9]{1,2})?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex MerchantRegex = new(
        @"\b(?:to|at|from)\s+(?<merchant>.+?)(?:\s+(?:via|on|using|ref|upi|account|card)\b|\.)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ReferenceRegex = new(
        @"\b(?:ref(?:erence)?(?:\s+no)?|upi\s+ref(?:\s+no)?|txn)\s*[:#]?\s*(?<reference>[A-Z0-9-]{6,})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

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
        var confidence = merchant == "Unknown" ? ParseConfidence.Medium : ParseConfidence.High;

        return new ParsedTransaction(
            new MoneyAmount(amount),
            merchant,
            OccurredAt: null,
            direction.Value,
            DetectSource(message),
            reference,
            confidence);
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
        if (!merchantMatch.Success)
        {
            return "Unknown";
        }

        return Regex.Replace(merchantMatch.Groups["merchant"].Value.Trim(), @"\s+", " ");
    }

    private static string? ExtractReference(string message)
    {
        var referenceMatch = ReferenceRegex.Match(message);
        return referenceMatch.Success ? referenceMatch.Groups["reference"].Value : null;
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
    }
}