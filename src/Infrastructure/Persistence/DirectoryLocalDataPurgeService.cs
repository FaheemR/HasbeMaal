namespace HasbeMaal.Infrastructure.Persistence;

public sealed class DirectoryLocalDataPurgeService : ILocalDataPurgeService
{
    private readonly string rootDirectory;

    public DirectoryLocalDataPurgeService(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Storage directory is required.", nameof(rootDirectory));
        }

        this.rootDirectory = Path.GetFullPath(rootDirectory);
        if (string.Equals(
            this.rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetPathRoot(this.rootDirectory)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Storage directory cannot be a filesystem root.", nameof(rootDirectory));
        }
    }

    public Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(rootDirectory);

        return Task.CompletedTask;
    }
}