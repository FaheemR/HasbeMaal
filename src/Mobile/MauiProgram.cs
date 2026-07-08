using HasbeMaal.Core.Parsing;
using HasbeMaal.Infrastructure.Persistence;
using HasbeMaal.Mobile.Views;
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
		builder.Services.AddSingleton<ILocalDataPurgeService>(_ =>
			new DirectoryLocalDataPurgeService(Path.Combine(FileSystem.AppDataDirectory, "local-data")));
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<TransactionsPage>();
		builder.Services.AddTransient<BudgetsPage>();
		builder.Services.AddTransient<GoalsPage>();
		builder.Services.AddTransient<SettingsPage>();

		return builder.Build();
	}
}
