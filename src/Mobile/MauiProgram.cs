using HasbeMaal.Core.Application;
using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Parsing;
using HasbeMaal.Infrastructure.Persistence;
using HasbeMaal.Mobile.Services;
using HasbeMaal.Mobile.Views;
using HasbeMaal.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace HasbeMaal.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<App>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<ISmsTransactionParser, DeterministicSmsTransactionParser>();
		#if ANDROID
		builder.Services.AddSingleton<ISmsPermissionService, AndroidSmsPermissionService>();
		#else
		builder.Services.AddSingleton<ISmsPermissionService, UnsupportedSmsPermissionService>();
		#endif
		builder.Services.AddSingleton<ISecureStorage>(_ => SecureStorage.Default);
		builder.Services.AddSingleton<IProtectedKeyValueStore, MauiSecureStorageProtectedKeyValueStore>();
		builder.Services.AddSingleton<IEncryptedStoreKeyProvider, PlatformEncryptedStoreKeyProvider>();
		builder.Services.AddSingleton<IEncryptedStore>(services =>
			new FileEncryptedStore(
				Path.Combine(FileSystem.AppDataDirectory, "local-data"),
				services.GetRequiredService<IEncryptedStoreKeyProvider>()));
		builder.Services.AddSingleton<ITransactionRepository, EncryptedTransactionRepository>();
		builder.Services.AddSingleton<ITransactionApplicationService, TransactionApplicationService>();
		builder.Services.AddSingleton<ILocalDataPurgeService>(_ =>
			new DirectoryLocalDataPurgeService(Path.Combine(FileSystem.AppDataDirectory, "local-data")));
		builder.Services.AddTransient<ManualTransactionEntryViewModel>();
		builder.Services.AddTransient<TransactionsViewModel>();
		builder.Services.AddTransient<BudgetsViewModel>();
		builder.Services.AddTransient<SmsPermissionConsentViewModel>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<TransactionsPage>();
		builder.Services.AddTransient<ManualEntryPage>();
		builder.Services.AddTransient<BudgetsPage>();
		builder.Services.AddTransient<GoalsPage>();
		builder.Services.AddTransient<SettingsPage>();

		return builder.Build();
	}
}
