using HasbeMaal.Mobile.Services;
using HasbeMaal.Presentation.ViewModels;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class BankSelectionPage : ContentPage
{
	private readonly BankSelectionViewModel viewModel;
	private readonly ILogger<BankSelectionPage> logger;

	public BankSelectionPage(
		BankSelectionViewModel viewModel,
		ILogger<BankSelectionPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(BankSelectionPage),
			nameof(InitializeComponent),
			"BankSelection");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(BankSelectionPage),
				nameof(InitializeComponent),
				"BankSelection");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(BankSelectionPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(BankSelectionPage),
			nameof(viewModel.LoadAsync),
			"BankSelection");

		try
		{
			await viewModel.LoadAsync();
			await entranceAnimation;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(BankSelectionPage),
				nameof(viewModel.LoadAsync),
				"BankSelection");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(BankSelectionPage), nameof(viewModel.LoadAsync), exception);
			throw;
		}
	}
}
