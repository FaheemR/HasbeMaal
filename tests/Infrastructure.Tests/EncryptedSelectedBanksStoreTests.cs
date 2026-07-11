using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class EncryptedSelectedBanksStoreTests
{
    [TestMethod]
    public async Task SetAsync_ThenGetAsync_RoundTripsSelection()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await store.SetAsync(["jkbank", "hdfc"]);

        var loaded = await store.GetAsync();
        Assert.HasCount(2, loaded);
        Assert.IsTrue(loaded.Contains("jkbank"));
        Assert.IsTrue(loaded.Contains("hdfc"));
    }

    [TestMethod]
    public async Task GetAsync_WhenNothingStored_ReturnsEmpty()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        Assert.IsEmpty(await store.GetAsync());
    }

    [TestMethod]
    public async Task SetAsync_OverwritesPreviousSelection()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        await store.SetAsync(["jkbank", "hdfc"]);

        await store.SetAsync(["sbi"]);

        var loaded = await store.GetAsync();
        var single = Assert.ContainsSingle(loaded);
        Assert.AreEqual("sbi", single);
    }

    [TestMethod]
    public async Task SetAsync_Empty_ThenGetAsync_ReturnsEmpty()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        await store.SetAsync(["jkbank"]);

        await store.SetAsync(Array.Empty<string>());

        Assert.IsEmpty(await store.GetAsync());
    }

    [TestMethod]
    public async Task SetAsync_Null_Throws()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await store.SetAsync(null!));
    }

    [TestMethod]
    public async Task Purge_RemovesPersistedSelection()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        await store.SetAsync(["jkbank", "hdfc"]);
        var purge = new DirectoryLocalDataPurgeService(directory.Path);

        await purge.PurgeAsync();

        Assert.IsEmpty(await store.GetAsync());
    }

    private static EncryptedSelectedBanksStore NewStore(string directory)
    {
        return new EncryptedSelectedBanksStore(NewFileStore(directory));
    }

    private static FileEncryptedStore NewFileStore(string directory)
    {
        var key = Enumerable.Repeat(0x42, 32).Select(value => (byte)value).ToArray();
        return new FileEncryptedStore(directory, key);
    }

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
                $"hasbemaal-selected-banks-store-test-{Guid.NewGuid():N}");

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
