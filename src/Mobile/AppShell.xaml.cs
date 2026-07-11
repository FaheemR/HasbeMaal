using HasbeMaal.Mobile.Views;
using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile;

public partial class AppShell : Shell
{
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<AppShell> logger;

	public AppShell(
		IServiceProvider serviceProvider,
		ILogger<AppShell> logger)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider);
		ArgumentNullException.ThrowIfNull(logger);

		this.serviceProvider = serviceProvider;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(AppShell),
			nameof(InitializeComponent));

		try
		{
			InitializeComponent();

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded",
				nameof(AppShell),
				nameof(InitializeComponent));

			SetContentTemplate("Dashboard", () =>
				serviceProvider.GetRequiredService<DashboardPage>());
			SetContentTemplate("Transactions", () =>
				serviceProvider.GetRequiredService<TransactionsPage>());
			SetContentTemplate("ManualEntry", () =>
				serviceProvider.GetRequiredService<ManualEntryPage>());
			SetContentTemplate("Budgets", () =>
				serviceProvider.GetRequiredService<BudgetsPage>());
			SetContentTemplate("Goals", () =>
				serviceProvider.GetRequiredService<GoalsPage>());
			SetContentTemplate("Settings", () =>
				serviceProvider.GetRequiredService<SettingsPage>());

			Routing.RegisterRoute("SmsImport", typeof(SmsImportPage));
			Routing.RegisterRoute("BankSelection", typeof(BankSelectionPage));
			Routing.RegisterRoute("TransactionDetail", typeof(TransactionDetailPage));

			Navigating += OnNavigating;
			Navigated += OnNavigated;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(AppShell), nameof(AppShell), exception);
			throw;
		}
	}

	private void SetContentTemplate(string route, Func<object> pageFactory)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(AppShell),
			nameof(SetContentTemplate),
			route);

		var shellContent = Items
			.SelectMany(item => item.Items)
			.SelectMany(section => section.Items)
			.Single(content => content.Route == route);

		shellContent.ContentTemplate = new DataTemplate(pageFactory);

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
			nameof(AppShell),
			nameof(SetContentTemplate),
			route);
	}

	private void OnNavigating(object? sender, ShellNavigatingEventArgs args)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(AppShell),
			nameof(OnNavigating),
			GetSafeRoute(args.Target));
	}

	private void OnNavigated(object? sender, ShellNavigatedEventArgs args)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
			nameof(AppShell),
			nameof(OnNavigated),
			GetSafeRoute(args.Current));
	}

	private static string GetSafeRoute(ShellNavigationState? state)
	{
		var route = state?.Location.OriginalString;
		if (string.IsNullOrWhiteSpace(route))
		{
			return "Unknown";
		}

		var queryStart = route.IndexOf('?', StringComparison.Ordinal);

		return queryStart < 0 ? route : route[..queryStart];
	}
}
