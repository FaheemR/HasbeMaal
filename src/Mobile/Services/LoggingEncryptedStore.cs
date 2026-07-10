using System.Collections;
using HasbeMaal.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Services;

public sealed class LoggingEncryptedStore : IEncryptedStore
{
	private readonly IEncryptedStore inner;
	private readonly ILogger<LoggingEncryptedStore> logger;

	public LoggingEncryptedStore(
		IEncryptedStore inner,
		ILogger<LoggingEncryptedStore> logger)
	{
		ArgumentNullException.ThrowIfNull(inner);
		ArgumentNullException.ThrowIfNull(logger);

		this.inner = inner;
		this.logger = logger;
	}

	public async Task SaveAsync<T>(
		string partitionKey,
		T value,
		CancellationToken cancellationToken = default)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started ItemCount={ItemCount}",
			nameof(LoggingEncryptedStore),
			nameof(SaveAsync),
			GetItemCount(value));

		try
		{
			await inner.SaveAsync(partitionKey, value, cancellationToken).ConfigureAwait(false);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded ItemCount={ItemCount}",
				nameof(LoggingEncryptedStore),
				nameof(SaveAsync),
				GetItemCount(value));
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingEncryptedStore), nameof(SaveAsync), exception);
			throw;
		}
	}

	public async Task<T?> LoadAsync<T>(
		string partitionKey,
		CancellationToken cancellationToken = default)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(LoggingEncryptedStore),
			nameof(LoadAsync));

		try
		{
			var value = await inner.LoadAsync<T>(partitionKey, cancellationToken).ConfigureAwait(false);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded HasValue={HasValue} ItemCount={ItemCount}",
				nameof(LoggingEncryptedStore),
				nameof(LoadAsync),
				value is not null,
				GetItemCount(value));

			return value;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingEncryptedStore), nameof(LoadAsync), exception);
			throw;
		}
	}

	private static int? GetItemCount<T>(T value) => value switch
	{
		null => null,
		ICollection collection => collection.Count,
		_ => null
	};
}