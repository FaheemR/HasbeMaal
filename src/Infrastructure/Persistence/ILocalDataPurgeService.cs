namespace HasbeMaal.Infrastructure.Persistence;

public interface ILocalDataPurgeService
{
    Task PurgeAsync(CancellationToken cancellationToken = default);
}