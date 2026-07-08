using System.Reflection;
using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Tests;

[TestClass]
public sealed class ITransactionRepositoryTests
{
    [TestMethod]
    public void RepositoryContract_IsDefinedInCoreDomain()
    {
        Assert.AreEqual("HasbeMaal.Core.Domain", typeof(ITransactionRepository).Namespace);
    }

    [TestMethod]
    public void RepositoryContract_SaveAsync_AcceptsTransactionAndCancellationToken()
    {
        var method = typeof(ITransactionRepository).GetMethod(nameof(ITransactionRepository.SaveAsync));

        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(Task), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.HasCount(2, parameters);
        Assert.AreEqual(typeof(FinancialTransaction), parameters[0].ParameterType);
        Assert.AreEqual(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [TestMethod]
    public void RepositoryContract_GetByIdAsync_ReturnsNullableTransactionTask()
    {
        var method = typeof(ITransactionRepository).GetMethod(nameof(ITransactionRepository.GetByIdAsync));

        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(Task<FinancialTransaction?>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.HasCount(2, parameters);
        Assert.AreEqual(typeof(Guid), parameters[0].ParameterType);
        Assert.AreEqual(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [TestMethod]
    public void RepositoryContract_ListAsync_ReturnsReadOnlyTransactionsForDateRange()
    {
        var method = typeof(ITransactionRepository).GetMethod(nameof(ITransactionRepository.ListAsync));

        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(Task<IReadOnlyList<FinancialTransaction>>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.HasCount(3, parameters);
        Assert.AreEqual(typeof(DateOnly), parameters[0].ParameterType);
        Assert.AreEqual(typeof(DateOnly), parameters[1].ParameterType);
        Assert.AreEqual(typeof(CancellationToken), parameters[2].ParameterType);
    }

    [TestMethod]
    public void RepositoryContract_DoesNotExposeStorageTypes()
    {
        var methods = typeof(ITransactionRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            Assert.DoesNotContain("Infrastructure", method.ReturnType.FullName ?? string.Empty);

            foreach (var parameter in method.GetParameters())
            {
                Assert.DoesNotContain("Infrastructure", parameter.ParameterType.FullName ?? string.Empty);
            }
        }
    }
}