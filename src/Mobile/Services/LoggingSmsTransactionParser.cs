using HasbeMaal.Core.Parsing;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Services;

public sealed class LoggingSmsTransactionParser : ISmsTransactionParser
{
	private readonly ISmsTransactionParser inner;
	private readonly ILogger<LoggingSmsTransactionParser> logger;

	public LoggingSmsTransactionParser(
		ISmsTransactionParser inner,
		ILogger<LoggingSmsTransactionParser> logger)
	{
		ArgumentNullException.ThrowIfNull(inner);
		ArgumentNullException.ThrowIfNull(logger);

		this.inner = inner;
		this.logger = logger;
	}

	public ParsedTransaction? TryParse(string message)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(LoggingSmsTransactionParser),
			nameof(TryParse));

		try
		{
			var parsedTransaction = inner.TryParse(message);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Parsed={Parsed} Confidence={Confidence}",
				nameof(LoggingSmsTransactionParser),
				nameof(TryParse),
				parsedTransaction is not null,
				parsedTransaction?.Confidence.ToString() ?? "None");

			return parsedTransaction;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(LoggingSmsTransactionParser), nameof(TryParse), exception);
			throw;
		}
	}
}