using HasbeMaal.Core.Import;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class BankSenderMatcherTests
{
    [TestMethod]
    [DataRow("JKBANK")]
    [DataRow("jkbank")]
    [DataRow("AD-JKBANK")]
    [DataRow("AD-JKBANK-S")]
    [DataRow("IM-JKBANK-T")]
    public void IsMatch_JkBankVariants_ReturnsTrue(string sender)
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(null);

        Assert.IsTrue(matcher.IsMatch(sender));
    }

    [TestMethod]
    [DataRow("HDFCBK")]
    [DataRow("HDFCBN")]
    [DataRow("AX-HDFCBK-S")]
    [DataRow("VM-HDFCBN")]
    [DataRow("ax-hdfcbk")]
    public void IsMatch_HdfcVariants_ReturnsTrue(string sender)
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(null);

        Assert.IsTrue(matcher.IsMatch(sender));
    }

    [TestMethod]
    [DataRow("JOHNXYZ")]
    [DataRow("AD-FLIPKT")]
    [DataRow("VK-RANDOM")]
    [DataRow("9876543210")]
    [DataRow("VM-AMAZON")]
    public void IsMatch_UnrelatedSenders_ReturnsFalse(string sender)
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(null);

        Assert.IsFalse(matcher.IsMatch(sender));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void IsMatch_NullEmptyOrWhitespace_ReturnsFalse(string? sender)
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(null);

        Assert.IsFalse(matcher.IsMatch(sender));
    }

    [TestMethod]
    [DataRow("S")]
    [DataRow("T")]
    [DataRow("P")]
    [DataRow("G")]
    public void IsMatch_StripsEachRouteSuffix(string route)
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(null);

        Assert.IsTrue(matcher.IsMatch($"AX-HDFCBK-{route}"));
    }

    [TestMethod]
    public void IsMatch_StripsBareRouteSuffixWithoutOperatorPrefix()
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(null);

        Assert.IsTrue(matcher.IsMatch("HDFCBK-S"));
    }

    [TestMethod]
    public void ForSelectedBanks_ScopesMatchingToSelection()
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(["jkbank"]);

        Assert.IsTrue(matcher.IsMatch("AD-JKBANK-S"));
        Assert.IsFalse(matcher.IsMatch("AX-HDFCBK"));
    }

    [TestMethod]
    public void ForSelectedBanks_UnknownSelection_MatchesNothing()
    {
        var matcher = BankSenderMatcher.ForSelectedBanks(["not-a-real-bank"]);

        Assert.IsFalse(matcher.IsMatch("AX-HDFCBK"));
        Assert.IsFalse(matcher.IsMatch("JKBANK"));
    }

    [TestMethod]
    public void Constructor_NullHeaders_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new BankSenderMatcher(null!));
    }

    [TestMethod]
    public void NormalizeHeader_StripsSeparatorsAndUppercases()
    {
        Assert.AreEqual("HDFCBK", BankSenderMatcher.NormalizeHeader("hdfc-bk"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("---")]
    public void NormalizeHeader_EmptyOrSeparatorOnly_ReturnsEmpty(string? header)
    {
        Assert.AreEqual(string.Empty, BankSenderMatcher.NormalizeHeader(header));
    }
}
