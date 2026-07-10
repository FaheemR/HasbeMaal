using HasbeMaal.Presentation.ViewModels;
using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class BudgetsPage : ContentPage
{
	private readonly BudgetsViewModel viewModel;
	private readonly ILogger<BudgetsPage> logger;

	public BudgetsPage(
		BudgetsViewModel viewModel,
		ILogger<BudgetsPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(BudgetsPage),
			nameof(InitializeComponent),
			"Budgets");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(BudgetsPage),
				nameof(InitializeComponent),
				"Budgets");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(BudgetsPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(BudgetsPage),
			nameof(viewModel.LoadAsync),
			"Budgets");

		try
		{
			await viewModel.LoadAsync();
			await entranceAnimation;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(BudgetsPage),
				nameof(viewModel.LoadAsync),
				"Budgets");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(BudgetsPage), nameof(viewModel.LoadAsync), exception);
			throw;
		}
	}
}