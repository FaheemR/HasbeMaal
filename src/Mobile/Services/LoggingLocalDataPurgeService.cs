using HasbeMaal.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Services;

public sealed class LoggingLocalDataPurgeService : ILocalDataPurgeService
{
	private readonly ILocalDataPurgeService inner;
	private readonly ILogger<LoggingLocalDataPurgeService> logger;

	public LoggingLocalDataPurgeService(
		ILocalDataPurgeService inner,
		ILogger<LoggingLocalDataPurgeService> logger)
	{
		ArgumentNullException.ThrowIfNull(inner);
		ArgumentNullException.ThrowIfNull(logger);

		this.inner = inner;
		this.logger = logger;
	}

	public async Task PurgeAsync(CancellationToken cancellationToken = default)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(LoggingLocalDataPurgeService),
			nameof(PurgeAsync));

		try
		{
			await inner.PurgeAsync(cancellationToken).ConfigureAwait(false);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded",
				nameof(LoggingLocalDataPurgeService),
				nameof(PurgeAsync));
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingLocalDataPurgeService), nameof(PurgeAsync), exception);
			throw;
		}
	}
}