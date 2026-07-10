using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Services;

public static class PrivacySafeLog
{
	private const string Unavailable = "Unavailable";
	private const string None = "None";

	public static void LogSanitizedException(
		this ILogger logger,
		string component,
		string operation,
		Exception exception)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(exception);

		// Do not pass the exception object to ILogger; exception messages can include sensitive runtime values.
		logger.LogError(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Failed ExceptionType={ExceptionType} InnerExceptionType={InnerExceptionType} ExceptionStackTrace={ExceptionStackTrace}",
			component,
			operation,
			GetExceptionType(exception),
			GetInnerExceptionType(exception),
			GetStackTrace(exception));
	}

	private static string GetExceptionType(Exception exception) =>
		exception.GetType().FullName ?? exception.GetType().Name;

	private static string GetInnerExceptionType(Exception exception) =>
		exception.InnerException is null ? None : GetExceptionType(exception.InnerException);

	private static string GetStackTrace(Exception exception)
	{
		var stackTrace = new StackTrace(exception, fNeedFileInfo: false).ToString();

		return string.IsNullOrWhiteSpace(stackTrace) ? Unavailable : stackTrace;
	}
}