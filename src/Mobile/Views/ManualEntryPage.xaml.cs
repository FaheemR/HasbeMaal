using HasbeMaal.Presentation.ViewModels;
using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class ManualEntryPage : ContentPage
{
	private readonly ILogger<ManualEntryPage> logger;

	public ManualEntryPage(
		ManualTransactionEntryViewModel viewModel,
		ILogger<ManualEntryPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(ManualEntryPage),
			nameof(InitializeComponent),
			"ManualEntry");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(ManualEntryPage),
				nameof(InitializeComponent),
				"ManualEntry");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(ManualEntryPage), nameof(InitializeComponent), exception);
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
			logger.LogSanitizedException(nameof(ManualEntryPage), nameof(OnAppearing), exception);
		}
	}
}