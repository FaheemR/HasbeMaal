#if ANDROID
using AndroidLog = Android.Util.Log;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Platforms.Android;

public sealed class AndroidLogLoggerProvider : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName) => new AndroidLogLogger(categoryName);

	public void Dispose()
	{
	}

	private sealed class AndroidLogLogger : ILogger
	{
		private const string Tag = "HasbeMaal";

		private readonly string categoryName;

		public AndroidLogLogger(string categoryName)
		{
			this.categoryName = categoryName;
		}

		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull => NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) =>
			logLevel != LogLevel.None
			&& categoryName.StartsWith("HasbeMaal.", StringComparison.Ordinal);

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var message = formatter(state, exception);
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			var logLine = $"{logLevel}: {categoryName}: {message}";
			Write(logLevel, logLine);
		}

		private static void Write(LogLevel logLevel, string message)
		{
			switch (logLevel)
			{
				case LogLevel.Trace:
					AndroidLog.Verbose(Tag, message);
					break;
				case LogLevel.Debug:
					AndroidLog.Debug(Tag, message);
					break;
				case LogLevel.Information:
					AndroidLog.Info(Tag, message);
					break;
				case LogLevel.Warning:
					AndroidLog.Warn(Tag, message);
					break;
				case LogLevel.Error:
					AndroidLog.Error(Tag, message);
					break;
				case LogLevel.Critical:
					AndroidLog.Wtf(Tag, message);
					break;
			}
		}
	}

	private sealed class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new();

		public void Dispose()
		{
		}
	}
}
#endif