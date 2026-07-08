using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class DirectoryLocalDataPurgeServiceTests
{
    [TestMethod]
    public async Task PurgeAsync_RemovesAllFilesAndDirectories()
    {
        using var directory = TemporaryDirectory.Create();
        Directory.CreateDirectory(Path.Combine(directory.Path, "nested"));
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "transactions.json"), "REDACTED STORE");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "raw-source-remnant.txt"), "SYNTH001");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "nested", "cache.tmp"), "XX0000");
        var service = new DirectoryLocalDataPurgeService(directory.Path);

        await service.PurgeAsync();

        Assert.IsTrue(Directory.Exists(directory.Path));
        Assert.IsEmpty(Directory.GetFileSystemEntries(directory.Path));
    }

    [TestMethod]
    public async Task PurgeAsync_MissingDirectory_CreatesEmptyDirectory()
    {
        using var directory = TemporaryDirectory.Create(createDirectory: false);
        var service = new DirectoryLocalDataPurgeService(directory.Path);

        await service.PurgeAsync();

        Assert.IsTrue(Directory.Exists(directory.Path));
        Assert.IsEmpty(Directory.GetFileSystemEntries(directory.Path));
    }

    [TestMethod]
    public async Task PurgeAsync_CanceledBeforeDelete_ThrowsAndKeepsExistingFiles()
    {
        using var directory = TemporaryDirectory.Create();
        var file = Path.Combine(directory.Path, "transactions.json");
        await File.WriteAllTextAsync(file, "REDACTED STORE");
        var service = new DirectoryLocalDataPurgeService(directory.Path);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await service.PurgeAsync(cancellation.Token));

        Assert.IsTrue(File.Exists(file));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankDirectory_Throws(string? rootDirectory)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new DirectoryLocalDataPurgeService(rootDirectory!));
    }

    [TestMethod]
    public void Constructor_FilesystemRoot_Throws()
    {
        var root = Path.GetPathRoot(Path.GetTempPath());

        Assert.IsNotNull(root);
        Assert.ThrowsExactly<ArgumentException>(() => new DirectoryLocalDataPurgeService(root));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create(bool createDirectory = true)
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hasbemaal-purge-test-{Guid.NewGuid():N}");

            if (createDirectory)
            {
                Directory.CreateDirectory(path);
            }

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