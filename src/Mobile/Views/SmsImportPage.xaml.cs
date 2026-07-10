using HasbeMaal.Mobile.Services;
using HasbeMaal.Presentation.ViewModels;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class SmsImportPage : ContentPage
{
	private readonly SmsImportViewModel viewModel;
	private readonly ILogger<SmsImportPage> logger;

	public SmsImportPage(
		SmsImportViewModel viewModel,
		ILogger<SmsImportPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(SmsImportPage),
			nameof(InitializeComponent),
			"SmsImport");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(SmsImportPage),
				nameof(InitializeComponent),
				"SmsImport");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(SmsImportPage), nameof(InitializeComponent), exception);
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
			logger.LogSanitizedException(nameof(SmsImportPage), nameof(OnAppearing), exception);
		}
	}

	private void OnConfidenceFilterChanged(object? sender, EventArgs e)
	{
		viewModel.ConfidenceFilter = ConfidenceFilterPicker.SelectedIndex switch
		{
			1 => SmsImportReviewFilter.Medium,
			2 => SmsImportReviewFilter.Low,
			_ => SmsImportReviewFilter.All
		};
	}
}
