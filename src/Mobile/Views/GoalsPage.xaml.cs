using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class GoalsPage : ContentPage
{
	private readonly ILogger<GoalsPage> logger;

	public GoalsPage(ILogger<GoalsPage> logger)
	{
		ArgumentNullException.ThrowIfNull(logger);
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(GoalsPage),
			nameof(InitializeComponent),
			"Goals");

		try
		{
			InitializeComponent();

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(GoalsPage),
				nameof(InitializeComponent),
				"Goals");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(GoalsPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		try
		{
			await this.AnimateEntranceAsync();
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(GoalsPage), nameof(OnAppearing), exception);
		}
	}
}