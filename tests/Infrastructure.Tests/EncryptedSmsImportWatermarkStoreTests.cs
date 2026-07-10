using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class EncryptedSmsImportWatermarkStoreTests
{
    [TestMethod]
    public async Task SetAsync_ThenGetAsync_RoundTripsWatermark()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        var watermark = new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero);

        await store.SetAsync(watermark);

        Assert.AreEqual(watermark, await store.GetAsync());
    }

    [TestMethod]
    public async Task GetAsync_WhenNothingStored_ReturnsNull()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);

        Assert.IsNull(await store.GetAsync());
    }

    [TestMethod]
    public async Task SetAsync_Null_ThenGetAsync_ReturnsNull()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        await store.SetAsync(new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero));

        await store.SetAsync(null);

        Assert.IsNull(await store.GetAsync());
    }

    [TestMethod]
    public async Task SetAsync_OverwritesPreviousWatermark()
    {
        using var directory = TemporaryDirectory.Create();
        var store = NewStore(directory.Path);
        await store.SetAsync(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        var latest = new DateTimeOffset(2026, 7, 10, 9, 30, 0, TimeSpan.Zero);

        await store.SetAsync(latest);

        Assert.AreEqual(latest, await store.GetAsync());
    }

    private static EncryptedSmsImportWatermarkStore NewStore(string directory)
    {
        return new EncryptedSmsImportWatermarkStore(NewFileStore(directory));
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
                $"hasbemaal-watermark-store-test-{Guid.NewGuid():N}");

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
