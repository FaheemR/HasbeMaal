using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Import;
using HasbeMaal.Core.Parsing;
using HasbeMaal.Core.Planning;
using HasbeMaal.Infrastructure.Persistence;
using HasbeMaal.Mobile.Services;
using HasbeMaal.Mobile.Views;
using HasbeMaal.Presentation.ViewModels;
#if ANDROID
using AndroidColor = Android.Graphics.Color;
using AndroidColorStateList = Android.Content.Res.ColorStateList;
using HasbeMaal.Mobile.Platforms.Android;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Storage;

namespace HasbeMaal.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		LogAndroidStartup(nameof(CreateMauiApp), "Started");

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureMauiHandlers(handlers =>
			{
#if ANDROID
				EntryHandler.Mapper.AppendToMapping("HasbeMaalInputChrome", (handler, _) =>
					RemoveNativeInputChrome(handler.PlatformView));
				DatePickerHandler.Mapper.AppendToMapping("HasbeMaalInputChrome", (handler, _) =>
					RemoveNativeInputChrome(handler.PlatformView));
#endif
			});

#if DEBUG
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#if ANDROID
		builder.Logging.AddProvider(new AndroidLogLoggerProvider());
#endif
#endif

		builder.Services.AddSingleton<App>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<DeterministicSmsTransactionParser>();
		builder.Services.AddSingleton<ISmsTransactionParser>(services =>
			new LoggingSmsTransactionParser(
				services.GetRequiredService<DeterministicSmsTransactionParser>(),
				services.GetRequiredService<ILogger<LoggingSmsTransactionParser>>()));
		#if ANDROID
		builder.Services.AddSingleton<ISmsPermissionService, AndroidSmsPermissionService>();
		#else
		builder.Services.AddSingleton<ISmsPermissionService, UnsupportedSmsPermissionService>();
		#endif
		#if ANDROID
		builder.Services.AddSingleton<ISmsInboxReader, AndroidSmsInboxReader>();
		#else
		builder.Services.AddSingleton<ISmsInboxReader, UnsupportedSmsInboxReader>();
		#endif
		builder.Services.AddSingleton<ISecureStorage>(_ => SecureStorage.Default);
		builder.Services.AddSingleton<IProtectedKeyValueStore, MauiSecureStorageProtectedKeyValueStore>();
		builder.Services.AddSingleton<IEncryptedStoreKeyProvider, PlatformEncryptedStoreKeyProvider>();
		builder.Services.AddSingleton<IEncryptedStore>(services =>
			new LoggingEncryptedStore(
				new FileEncryptedStore(
					Path.Combine(FileSystem.AppDataDirectory, "local-data"),
					services.GetRequiredService<IEncryptedStoreKeyProvider>()),
				services.GetRequiredService<ILogger<LoggingEncryptedStore>>()));
		builder.Services.AddSingleton<ITransactionRepository, EncryptedTransactionRepository>();
		builder.Services.AddSingleton<IMonthlyBudgetCategoryRepository, EncryptedMonthlyBudgetCategoryRepository>();
		builder.Services.AddSingleton<ISmsImportWatermarkStore, EncryptedSmsImportWatermarkStore>();
		builder.Services.AddSingleton<ISelectedBanksStore, EncryptedSelectedBanksStore>();
		builder.Services.AddSingleton<ISmsTransactionImporter, SmsTransactionImporter>();
		builder.Services.AddSingleton<TransactionApplicationService>();
		builder.Services.AddSingleton<ITransactionApplicationService>(services =>
			new LoggingTransactionApplicationService(
				services.GetRequiredService<TransactionApplicationService>(),
				services.GetRequiredService<ILogger<LoggingTransactionApplicationService>>()));
		builder.Services.AddSingleton<ILocalDataPurgeService>(services =>
			new LoggingLocalDataPurgeService(
				new DirectoryLocalDataPurgeService(Path.Combine(FileSystem.AppDataDirectory, "local-data")),
				services.GetRequiredService<ILogger<LoggingLocalDataPurgeService>>()));
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<ManualTransactionEntryViewModel>();
		builder.Services.AddTransient<TransactionsViewModel>();
		builder.Services.AddTransient<BudgetsViewModel>();
		builder.Services.AddTransient<SmsPermissionConsentViewModel>();
		builder.Services.AddTransient<SmsImportViewModel>();
		builder.Services.AddTransient<BankSelectionViewModel>();
		builder.Services.AddTransient<TransactionDetailViewModel>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<TransactionsPage>();
		builder.Services.AddTransient<ManualEntryPage>();
		builder.Services.AddTransient<BudgetsPage>();
		builder.Services.AddTransient<GoalsPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SmsImportPage>();
		builder.Services.AddTransient<BankSelectionPage>();
		builder.Services.AddTransient<TransactionDetailPage>();

		LogAndroidStartup("Build", "Started");

		MauiApp app;
		try
		{
			app = builder.Build();
		}
		catch (Exception exception)
		{
			LogAndroidStartupFailure("Build", exception);
			throw;
		}

		LogAndroidStartup("Build", "Succeeded");

		var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MauiProgram).FullName ?? nameof(MauiProgram));
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded",
			nameof(MauiProgram),
			nameof(CreateMauiApp));
		RegisterUnhandledExceptionLogging(logger);

		return app;
	}

	private static void LogAndroidStartup(string operation, string status)
	{
#if DEBUG && ANDROID
		global::Android.Util.Log.Debug(
			"HasbeMaal",
			$"Debug: HasbeMaal.Mobile.MauiProgram: HasbeMaal diagnostic Component={nameof(MauiProgram)} Operation={operation} Status={status}");
#endif
	}

#if ANDROID
	private static void RemoveNativeInputChrome(global::Android.Views.View platformView)
	{
		platformView.BackgroundTintList = AndroidColorStateList.ValueOf(AndroidColor.Transparent);
	}
#endif

	private static void LogAndroidStartupFailure(string operation, Exception exception)
	{
#if DEBUG && ANDROID
		global::Android.Util.Log.Error(
			"HasbeMaal",
			$"Error: HasbeMaal.Mobile.MauiProgram: HasbeMaal diagnostic Component={nameof(MauiProgram)} Operation={operation} Status=Failed ExceptionType={exception.GetType().FullName ?? exception.GetType().Name}");
#endif
	}

	private static void RegisterUnhandledExceptionLogging(ILogger logger)
	{
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			if (args.ExceptionObject is Exception exception)
			{
				logger.LogSanitizedException(nameof(AppDomain), nameof(AppDomain.UnhandledException), exception);
			}
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			logger.LogSanitizedException(nameof(TaskScheduler), nameof(TaskScheduler.UnobservedTaskException), args.Exception);
		};
	}
}
