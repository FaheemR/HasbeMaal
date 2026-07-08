using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class MoneyAmountTests
{
    [TestMethod]
    public void Constructor_DefaultsToInr()
    {
        var money = new MoneyAmount(100m);

        Assert.AreEqual("INR", money.Currency);
    }

    [TestMethod]
    public void Constructor_NormalizesCurrencyToUpperInvariant()
    {
        var money = new MoneyAmount(100m, " usd ");

        Assert.AreEqual("USD", money.Currency);
    }

    [TestMethod]
    public void Constructor_AllowsZero()
    {
        var money = new MoneyAmount(0m);

        Assert.AreEqual(0m, money.Amount);
    }

    [TestMethod]
    public void Constructor_NegativeAmount_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MoneyAmount(-0.01m));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankCurrency_Throws(string currency)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new MoneyAmount(100m, currency));
    }

    [TestMethod]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Assert.AreEqual(new MoneyAmount(50.25m), new MoneyAmount(50.25m, "inr"));
    }
}
