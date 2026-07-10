using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class SmsSenderAllowlistTests
{
    [TestMethod]
    [DataRow("AX-HDFCBK")]
    [DataRow("VM-ICICIB")]
    [DataRow("JD-SBIINB")]
    public void IsAllowed_KnownBankHeaderWithOperatorPrefix_ReturnsTrue(string sender)
    {
        var allowlist = new SmsSenderAllowlist();

        Assert.IsTrue(allowlist.IsAllowed(sender));
    }

    [TestMethod]
    public void IsAllowed_BareBankHeader_ReturnsTrue()
    {
        var allowlist = new SmsSenderAllowlist();

        Assert.IsTrue(allowlist.IsAllowed("HDFCBK"));
    }

    [TestMethod]
    [DataRow("JOHNXYZ")]
    [DataRow("AD-FLIPKT")]
    [DataRow("VK-RANDOM")]
    public void IsAllowed_UnrelatedSender_ReturnsFalse(string sender)
    {
        var allowlist = new SmsSenderAllowlist();

        Assert.IsFalse(allowlist.IsAllowed(sender));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void IsAllowed_NullEmptyOrWhitespace_ReturnsFalse(string? sender)
    {
        var allowlist = new SmsSenderAllowlist();

        Assert.IsFalse(allowlist.IsAllowed(sender));
    }

    [TestMethod]
    [DataRow("ax-hdfcbk")]
    [DataRow("Ax-HdFcBk")]
    public void IsAllowed_IsCaseInsensitive(string sender)
    {
        var allowlist = new SmsSenderAllowlist();

        Assert.IsTrue(allowlist.IsAllowed(sender));
    }

    [TestMethod]
    public void IsAllowed_AdditionalCode_MatchesCustomAndStillMatchesDefaults()
    {
        var allowlist = new SmsSenderAllowlist(["MYBANK"]);

        Assert.IsTrue(allowlist.IsAllowed("VK-MYBANK"), "custom code should match");
        Assert.IsTrue(allowlist.IsAllowed("HDFCBK"), "defaults should still match");
    }

    [TestMethod]
    public void Constructor_AdditionalCode_IsNormalizedBeforeMerge()
    {
        var allowlist = new SmsSenderAllowlist(["my-bank"]);

        Assert.IsTrue(allowlist.IsAllowed("JM-MYBANK"));
    }

    [TestMethod]
    public void Constructor_TooShortAdditionalCode_IsIgnored()
    {
        var allowlist = new SmsSenderAllowlist(["AB"]);

        Assert.IsFalse(allowlist.IsAllowed("VK-XXABXX"));
    }

    [TestMethod]
    public void Constructor_NullAdditionalCodes_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new SmsSenderAllowlist(null!));
    }

    [TestMethod]
    public void Codes_DefaultsSeeded_AreAllNormalizedAndAtLeastFiveChars()
    {
        var allowlist = new SmsSenderAllowlist();

        Assert.IsNotEmpty(allowlist.Codes);
        foreach (var code in allowlist.Codes)
        {
            Assert.IsGreaterThanOrEqualTo(5, code.Length, $"code '{code}' should be at least five chars");
            Assert.AreEqual(code.ToUpperInvariant(), code, $"code '{code}' should be uppercase");
        }
    }
}
