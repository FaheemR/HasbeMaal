using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Services;

public sealed class LoggingTransactionApplicationService : ITransactionApplicationService
{
	private readonly ITransactionApplicationService inner;
	private readonly ILogger<LoggingTransactionApplicationService> logger;

	public LoggingTransactionApplicationService(
		ITransactionApplicationService inner,
		ILogger<LoggingTransactionApplicationService> logger)
	{
		ArgumentNullException.ThrowIfNull(inner);
		ArgumentNullException.ThrowIfNull(logger);

		this.inner = inner;
		this.logger = logger;
	}

	public async Task<TransactionSaveResult> SaveAsync(
		FinancialTransaction transaction,
		CancellationToken cancellationToken = default)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(LoggingTransactionApplicationService),
			nameof(SaveAsync));

		try
		{
			var result = await inner.SaveAsync(transaction, cancellationToken).ConfigureAwait(false);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded SaveStatus={SaveStatus}",
				nameof(LoggingTransactionApplicationService),
				nameof(SaveAsync),
				result.Status.ToString());

			return result;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingTransactionApplicationService), nameof(SaveAsync), exception);
			throw;
		}
	}

	public async Task<FinancialTransaction?> GetByIdAsync(
		Guid id,
		CancellationToken cancellationToken = default)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(LoggingTransactionApplicationService),
			nameof(GetByIdAsync));

		try
		{
			var transaction = await inner.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Found={Found}",
				nameof(LoggingTransactionApplicationService),
				nameof(GetByIdAsync),
				transaction is not null);

			return transaction;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingTransactionApplicationService), nameof(GetByIdAsync), exception);
			throw;
		}
	}

	public async Task<IReadOnlyList<FinancialTransaction>> ListAsync(
		DateOnly from,
		DateOnly to,
		CancellationToken cancellationToken = default)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started FromYear={FromYear} FromMonth={FromMonth} ToYear={ToYear} ToMonth={ToMonth}",
			nameof(LoggingTransactionApplicationService),
			nameof(ListAsync),
			from.Year,
			from.Month,
			to.Year,
			to.Month);

		try
		{
			var transactions = await inner.ListAsync(from, to, cancellationToken).ConfigureAwait(false);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Count={Count} FromYear={FromYear} FromMonth={FromMonth} ToYear={ToYear} ToMonth={ToMonth}",
				nameof(LoggingTransactionApplicationService),
				nameof(ListAsync),
				transactions.Count,
				from.Year,
				from.Month,
				to.Year,
				to.Month);

			return transactions;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingTransactionApplicationService), nameof(ListAsync), exception);
			throw;
		}
	}
}