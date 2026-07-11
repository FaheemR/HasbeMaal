namespace HasbeMaal.Core.Import;

/// <summary>
/// Curated, deterministic registry of banks and UPI apps mapped to their official DLT sender
/// header tokens. All entries are PUBLIC alphanumeric SMS sender identifiers; the registry contains
/// no user data. Used to resolve a user's bank selection into the header set a
/// <see cref="BankSenderMatcher"/> accepts.
/// </summary>
public static class BankSenderRegistry
{
    private static readonly IReadOnlyList<BankSender> BankList = BuildBanks();

    private static readonly IReadOnlyDictionary<string, BankSender> BanksById =
        BankList.ToDictionary(bank => bank.Id, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<string> AllHeaders =
        BankList.SelectMany(bank => bank.Headers).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>All curated banks and UPI apps, in registry order.</summary>
    public static IReadOnlyList<BankSender> Banks => BankList;

    /// <summary>Looks up a bank by its stable id (case-insensitive).</summary>
    public static bool TryGet(string bankId, out BankSender? bank)
    {
        if (!string.IsNullOrWhiteSpace(bankId) && BanksById.TryGetValue(bankId.Trim(), out var found))
        {
            bank = found;
            return true;
        }

        bank = null;
        return false;
    }

    /// <summary>
    /// Resolves the header set to match against for the given selection. A null or empty selection
    /// returns the full registry so scanning still works before the user picks banks. A non-empty
    /// selection returns the union of headers for the recognized ids; unknown ids are ignored.
    /// </summary>
    public static IReadOnlyList<string> ResolveHeaders(IReadOnlyCollection<string>? selectedBankIds)
    {
        if (selectedBankIds is null || selectedBankIds.Count == 0)
        {
            return AllHeaders;
        }

        var resolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var bankId in selectedBankIds)
        {
            if (bankId is not null && BanksById.TryGetValue(bankId.Trim(), out var bank))
            {
                foreach (var header in bank.Headers)
                {
                    resolved.Add(header);
                }
            }
        }

        return resolved.ToArray();
    }

    private static IReadOnlyList<BankSender> BuildBanks() =>
    [
        new BankSender("jkbank", "J&K Bank", ["JKBANK"]),
        new BankSender("hdfc", "HDFC Bank", ["HDFCBK", "HDFCBN"]),
        new BankSender("sbi", "State Bank of India", ["SBIINB", "SBICRD", "SBIBNK", "SBIUPI"]),
        new BankSender("icici", "ICICI Bank", ["ICICIB", "ICICIT"]),
        new BankSender("axis", "Axis Bank", ["AXISBK", "AXISBN"]),
        new BankSender("kotak", "Kotak Mahindra Bank", ["KOTAKB"]),
        new BankSender("pnb", "Punjab National Bank", ["PNBSMS", "PNBBNK"]),
        new BankSender("bob", "Bank of Baroda", ["BOBTXN", "BOBSMS"]),
        new BankSender("canara", "Canara Bank", ["CANBNK"]),
        new BankSender("idfc", "IDFC First Bank", ["IDFCFB", "IDFCBK"]),
        new BankSender("indusind", "IndusInd Bank", ["INDUSB"]),
        new BankSender("rbl", "RBL Bank", ["RBLBNK"]),
        new BankSender("union", "Union Bank of India", ["UNIONB"]),
        new BankSender("yes", "Yes Bank", ["YESBNK"]),
        new BankSender("federal", "Federal Bank", ["FEDBNK"]),
        new BankSender("central", "Central Bank of India", ["CENTBK"]),
        new BankSender("citi", "Citibank", ["CITIBK"]),
        new BankSender("hsbc", "HSBC", ["HSBCIN"]),
        new BankSender("sc", "Standard Chartered", ["SCBANK"]),
        new BankSender("amex", "American Express", ["AMEXIN"]),
        new BankSender("paytm", "Paytm", ["PAYTM", "PAYTMB"]),
        new BankSender("phonepe", "PhonePe", ["PHONPE"]),
        new BankSender("bhim", "BHIM UPI", ["BHIMUP"]),
        new BankSender("mobikwik", "MobiKwik", ["MOBIKW"]),
        new BankSender("amazonpay", "Amazon Pay", ["AMZNPY"]),
    ];
}
