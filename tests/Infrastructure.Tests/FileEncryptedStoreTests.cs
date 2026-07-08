using System.Security.Cryptography;
using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class FileEncryptedStoreTests
{
    [TestMethod]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsStructuredValue()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        var value = new StoredFixture("REDACTED STORE", 125.75m);

        await store.SaveAsync("transactions:2026-07", value);

        var loaded = await store.LoadAsync<StoredFixture>("transactions:2026-07");

        Assert.AreEqual(value, loaded);
    }

    [TestMethod]
    public async Task SaveAsync_DoesNotWritePlaintextValue()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await store.SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));

        var file = Assert.ContainsSingle(Directory.GetFiles(directory.Path));
        var contents = await File.ReadAllTextAsync(file);

        Assert.DoesNotContain("REDACTED STORE", contents);
        Assert.DoesNotContain("125.75", contents);
    }

    [TestMethod]
    public async Task SaveAsync_OverwriteLeavesOnlyFinalPayloadFile()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await store.SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));
        await store.SaveAsync("transactions:2026-07", new StoredFixture("REDACTED SCHOOL", 450m));

        var file = Assert.ContainsSingle(Directory.GetFiles(directory.Path));
        var loaded = await store.LoadAsync<StoredFixture>("transactions:2026-07");

        Assert.EndsWith(".json", file);
        Assert.AreEqual(new StoredFixture("REDACTED SCHOOL", 450m), loaded);
    }

    [TestMethod]
    public async Task SaveAsync_UsesHashedPartitionFileName()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await store.SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));

        var fileName = Path.GetFileName(Assert.ContainsSingle(Directory.GetFiles(directory.Path)));

        Assert.DoesNotContain("transactions", fileName);
        Assert.DoesNotContain("2026", fileName);
        Assert.EndsWith(".json", fileName);
    }

    [TestMethod]
    public async Task SaveAsync_FileNameDependsOnKey()
    {
        using var firstDirectory = TemporaryDirectory.Create();
        using var secondDirectory = TemporaryDirectory.Create();

        await NewStore(firstDirectory.Path, keyByte: 0x11)
            .SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));
        await NewStore(secondDirectory.Path, keyByte: 0x22)
            .SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));

        var firstFileName = Path.GetFileName(Assert.ContainsSingle(Directory.GetFiles(firstDirectory.Path)));
        var secondFileName = Path.GetFileName(Assert.ContainsSingle(Directory.GetFiles(secondDirectory.Path)));

        Assert.AreNotEqual(firstFileName, secondFileName);
    }

    [TestMethod]
    public async Task LoadAsync_FileCopiedToDifferentPartition_ThrowsAuthenticationTagMismatchException()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await store.SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));
        var julyFile = Assert.ContainsSingle(Directory.GetFiles(directory.Path));

        await store.SaveAsync("transactions:2026-08", new StoredFixture("REDACTED SCHOOL", 450m));
        var augustFile = Directory.GetFiles(directory.Path).Single(file => file != julyFile);

        File.Copy(julyFile, augustFile, overwrite: true);

        await Assert.ThrowsExactlyAsync<AuthenticationTagMismatchException>(async () =>
            await store.LoadAsync<StoredFixture>("transactions:2026-08"));
    }

    [TestMethod]
    public async Task LoadAsync_MissingPartition_ReturnsNull()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        var loaded = await store.LoadAsync<StoredFixture>("transactions:missing");

        Assert.IsNull(loaded);
    }

    [TestMethod]
    public async Task LoadAsync_WithDifferentKey_ReturnsNull()
    {
        using var directory = TemporaryDirectory.Create();
        await NewStore(directory.Path, keyByte: 0x11)
            .SaveAsync("transactions:2026-07", new StoredFixture("REDACTED STORE", 125.75m));

        var wrongKeyStore = NewStore(directory.Path, keyByte: 0x22);

        var loaded = await wrongKeyStore.LoadAsync<StoredFixture>("transactions:2026-07");

        Assert.IsNull(loaded);
    }

    [TestMethod]
    public void Constructor_BlankDirectory_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => NewStore("   "));
    }

    [TestMethod]
    public void Constructor_InvalidKeyLength_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new FileEncryptedStore(Path.GetTempPath(), new byte[16]));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public async Task SaveAsync_InvalidPartitionKey_Throws(string? partitionKey)
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await store.SaveAsync(partitionKey!, new StoredFixture("REDACTED STORE", 125.75m)));
    }

    [TestMethod]
    public async Task SaveAsync_NullValue_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await store.SaveAsync<StoredFixture>("transactions:2026-07", null!));
    }

    private static FileEncryptedStore NewStore(string directory, byte keyByte = 0x42)
    {
        var key = Enumerable.Repeat(keyByte, 32).Select(value => (byte)value).ToArray();
        return new FileEncryptedStore(directory, key);
    }

    private sealed record StoredFixture(string Merchant, decimal Amount);

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hasbemaal-test-{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}