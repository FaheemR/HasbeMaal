using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile;

public partial class App : Application
{
	private readonly AppShell appShell;
	private readonly ILogger<App> logger;

	public App(
		AppShell appShell,
		ILogger<App> logger)
	{
		ArgumentNullException.ThrowIfNull(appShell);
		ArgumentNullException.ThrowIfNull(logger);

		this.appShell = appShell;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(App),
			nameof(InitializeComponent));

		try
		{
			InitializeComponent();

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded",
				nameof(App),
				nameof(InitializeComponent));
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(App), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started",
			nameof(App),
			nameof(CreateWindow));

		try
		{
			var window = new Window(appShell);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded",
				nameof(App),
				nameof(CreateWindow));

			return window;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(App), nameof(CreateWindow), exception);
			throw;
		}
	}
}