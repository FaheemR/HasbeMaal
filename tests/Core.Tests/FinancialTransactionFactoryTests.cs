using System.Security.Cryptography;
using System.Text;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class FinancialTransactionFactoryTests
{
    private static ParsedTransaction NewParsed(string? reference) => new(
        new MoneyAmount(245.50m),
        "REDACTED STORE",
        new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
        TransactionDirection.Debit,
        TransactionSource.UpiSms,
        reference,
        ParseConfidence.High);

    [TestMethod]
    public void Create_MapsCoreFields()
    {
        var parsed = NewParsed("SYNTH-REF-001");
        var id = Guid.NewGuid();

        var tx = FinancialTransactionFactory.Create(parsed, "Groceries", id);

        Assert.AreEqual(id, tx.Id);
        Assert.AreEqual(245.50m, tx.Amount.Amount);
        Assert.AreEqual(TransactionDirection.Debit, tx.Direction);
        Assert.AreEqual(TransactionSource.UpiSms, tx.Source);
        Assert.AreEqual(parsed.OccurredAt, tx.OccurredAt);
        Assert.AreEqual("REDACTED STORE", tx.Merchant);
        Assert.AreEqual("Groceries", tx.Category);
    }

    [TestMethod]
    public void Create_HashesReference_WithoutExposingRawValueInHash()
    {
        const string reference = "SYNTH-REF-002";
        var parsed = NewParsed(reference);

        var tx = FinancialTransactionFactory.Create(parsed);

        Assert.IsNotNull(tx.SourceReferenceHash);
        Assert.AreNotEqual(reference, tx.SourceReferenceHash);
        StringAssert.DoesNotMatch(tx.SourceReferenceHash!, new System.Text.RegularExpressions.Regex(reference));
    }

    [TestMethod]
    public void Create_RetainsRawReference_ForDisplay()
    {
        const string reference = "SYNTH-REF-002";
        var parsed = NewParsed(reference);

        var tx = FinancialTransactionFactory.Create(parsed);

        Assert.AreEqual(reference, tx.SourceReference);
    }

    [TestMethod]
    public void Create_NullReference_LeavesReferenceAndHashNull()
    {
        var tx = FinancialTransactionFactory.Create(NewParsed(reference: null));

        Assert.IsNull(tx.SourceReference);
        Assert.IsNull(tx.SourceReferenceHash);
    }

    [TestMethod]
    public void Create_MapsAccountAndSourceMessage()
    {
        var parsed = new ParsedTransaction(
            new MoneyAmount(1234.00m),
            "Synthpay",
            new DateTimeOffset(2026, 7, 10, 20, 8, 0, TimeSpan.FromHours(5.5)),
            TransactionDirection.Credit,
            TransactionSource.CreditCardSms,
            Reference: null,
            ParseConfidence.High,
            OccurredOn: new DateOnly(2026, 7, 10),
            Account: "REDACTED Bank Credit Card ••0000",
            SourceMessage: "SYNTH ORIGINAL SMS BODY XX0000");

        var tx = FinancialTransactionFactory.Create(parsed);

        Assert.AreEqual("REDACTED Bank Credit Card ••0000", tx.Account);
        Assert.AreEqual("SYNTH ORIGINAL SMS BODY XX0000", tx.SourceMessage);
    }

    [TestMethod]
    public void Create_UsesDefaultCategory_WhenNotProvided()
    {
        var tx = FinancialTransactionFactory.Create(NewParsed("SYNTH-REF-003"));

        Assert.AreEqual("Uncategorized", tx.Category);
    }

    [TestMethod]
    public void Create_GeneratesId_WhenNotProvided()
    {
        var tx = FinancialTransactionFactory.Create(NewParsed("SYNTH-REF-004"));

        Assert.AreNotEqual(Guid.Empty, tx.Id);
    }

    [TestMethod]
    public void Create_NullParsed_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => FinancialTransactionFactory.Create(null!));
    }

    [TestMethod]
    public void HashReference_IsDeterministic()
    {
        var first = FinancialTransactionFactory.HashReference("SYNTH-REF-005");
        var second = FinancialTransactionFactory.HashReference("SYNTH-REF-005");

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void HashReference_NormalizesTrimAndCase()
    {
        var canonical = FinancialTransactionFactory.HashReference("SYNTH-REF-006");
        var messy = FinancialTransactionFactory.HashReference("  synth-ref-006  ");

        Assert.AreEqual(canonical, messy);
    }

    [TestMethod]
    public void HashReference_MatchesExpectedSha256OfNormalizedValue()
    {
        const string reference = "SYNTH-REF-007";
        var expected = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(reference.Trim().ToUpperInvariant())));

        var actual = FinancialTransactionFactory.HashReference(reference);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void HashReference_DifferentReferences_ProduceDifferentHashes()
    {
        var a = FinancialTransactionFactory.HashReference("SYNTH-REF-008");
        var b = FinancialTransactionFactory.HashReference("SYNTH-REF-009");

        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void HashReference_NullOrWhitespace_ReturnsNull(string? reference)
    {
        Assert.IsNull(FinancialTransactionFactory.HashReference(reference));
    }

    [TestMethod]
    public void Create_NullReference_YieldsNullHash()
    {
        var tx = FinancialTransactionFactory.Create(NewParsed(null));

        Assert.IsNull(tx.SourceReferenceHash);
    }
}
