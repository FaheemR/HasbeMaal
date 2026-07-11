using HasbeMaal.Core.Import;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class BankSenderRegistryTests
{
    [TestMethod]
    public void Banks_ContainsJkBank_WithJkbankHeader()
    {
        var jkBank = Assert.ContainsSingle(BankSenderRegistry.Banks.Where(bank => bank.Id == "jkbank"));

        Assert.IsTrue(jkBank.Headers.Contains("JKBANK"));
    }

    [TestMethod]
    public void Banks_ContainsHdfc_WithBothHeaderVariants()
    {
        var hdfc = Assert.ContainsSingle(BankSenderRegistry.Banks.Where(bank => bank.Id == "hdfc"));

        Assert.IsTrue(hdfc.Headers.Contains("HDFCBK"));
        Assert.IsTrue(hdfc.Headers.Contains("HDFCBN"));
    }

    [TestMethod]
    [DataRow("sbi")]
    [DataRow("icici")]
    [DataRow("axis")]
    [DataRow("kotak")]
    [DataRow("pnb")]
    [DataRow("bob")]
    [DataRow("canara")]
    [DataRow("idfc")]
    [DataRow("indusind")]
    [DataRow("rbl")]
    [DataRow("union")]
    [DataRow("yes")]
    [DataRow("federal")]
    [DataRow("paytm")]
    [DataRow("phonepe")]
    [DataRow("bhim")]
    public void Banks_ContainsRequiredSender(string bankId)
    {
        Assert.ContainsSingle(BankSenderRegistry.Banks.Where(bank => bank.Id == bankId));
    }

    [TestMethod]
    public void Banks_HaveUniqueIds()
    {
        var ids = BankSenderRegistry.Banks.Select(bank => bank.Id).ToArray();

        Assert.AreEqual(ids.Distinct(StringComparer.OrdinalIgnoreCase).Count(), ids.Length);
    }

    [TestMethod]
    public void Banks_HaveOnlyUppercaseAlphanumericHeaders()
    {
        foreach (var header in BankSenderRegistry.Banks.SelectMany(bank => bank.Headers))
        {
            Assert.AreEqual(header.ToUpperInvariant(), header, $"header '{header}' must be uppercase");
            Assert.IsTrue(header.All(char.IsAsciiLetterOrDigit), $"header '{header}' must be alphanumeric");
        }
    }

    [TestMethod]
    public void ResolveHeaders_EmptySelection_ReturnsFullRegistry()
    {
        var expected = BankSenderRegistry.Banks
            .SelectMany(bank => bank.Headers)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var resolved = BankSenderRegistry.ResolveHeaders(Array.Empty<string>());

        Assert.HasCount(expected, resolved);
    }

    [TestMethod]
    public void ResolveHeaders_NullSelection_ReturnsFullRegistry()
    {
        Assert.IsNotEmpty(BankSenderRegistry.ResolveHeaders(null));
    }

    [TestMethod]
    public void ResolveHeaders_SingleBank_ReturnsOnlyThatBanksHeaders()
    {
        var jkBank = BankSenderRegistry.Banks.Single(bank => bank.Id == "jkbank");

        var resolved = BankSenderRegistry.ResolveHeaders(["jkbank"]);

        Assert.HasCount(jkBank.Headers.Count, resolved);
        foreach (var header in jkBank.Headers)
        {
            Assert.IsTrue(resolved.Contains(header));
        }

        Assert.IsFalse(resolved.Contains("HDFCBK"));
    }

    [TestMethod]
    public void ResolveHeaders_UnknownBank_ReturnsEmpty()
    {
        Assert.IsEmpty(BankSenderRegistry.ResolveHeaders(["not-a-real-bank"]));
    }

    [TestMethod]
    public void TryGet_KnownBank_ReturnsTrueAndBank()
    {
        var found = BankSenderRegistry.TryGet("HDFC", out var bank);

        Assert.IsTrue(found);
        Assert.IsNotNull(bank);
        Assert.AreEqual("hdfc", bank.Id);
    }

    [TestMethod]
    public void TryGet_UnknownBank_ReturnsFalse()
    {
        Assert.IsFalse(BankSenderRegistry.TryGet("nope", out var bank));
        Assert.IsNull(bank);
    }
}
