using HasbeMaal.Presentation.ViewModels;
using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class DashboardPage : ContentPage
{
	private readonly DashboardViewModel viewModel;
	private readonly ILogger<DashboardPage> logger;

	public DashboardPage(
		DashboardViewModel viewModel,
		ILogger<DashboardPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(DashboardPage),
			nameof(InitializeComponent),
			"Dashboard");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(DashboardPage),
				nameof(InitializeComponent),
				"Dashboard");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(DashboardPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(DashboardPage),
			nameof(viewModel.LoadAsync),
			"Dashboard");

		try
		{
			await viewModel.LoadAsync();
			await entranceAnimation;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(DashboardPage),
				nameof(viewModel.LoadAsync),
				"Dashboard");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(DashboardPage), nameof(viewModel.LoadAsync), exception);
			throw;
		}
	}

	private async void OnAddEntryClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//ManualEntry");
	}

	private async void OnPlanningClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//Planning/Budgets");
	}
}