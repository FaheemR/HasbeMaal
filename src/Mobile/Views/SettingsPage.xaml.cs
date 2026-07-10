using HasbeMaal.Infrastructure.Persistence;
using HasbeMaal.Mobile.Services;
using HasbeMaal.Presentation.ViewModels;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class SettingsPage : ContentPage
{
	private readonly ILocalDataPurgeService localDataPurgeService;
	private readonly ILogger<SettingsPage> logger;
	private readonly SmsPermissionConsentViewModel viewModel;

	public SettingsPage(
		ILocalDataPurgeService localDataPurgeService,
		SmsPermissionConsentViewModel viewModel,
		ILogger<SettingsPage> logger)
	{
		ArgumentNullException.ThrowIfNull(localDataPurgeService);
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.localDataPurgeService = localDataPurgeService;
		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(SettingsPage),
			nameof(InitializeComponent),
			"Settings");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(SettingsPage),
				nameof(InitializeComponent),
				"Settings");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(SettingsPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(SettingsPage),
			nameof(viewModel.RefreshAsync),
			"Settings");

		try
		{
			await viewModel.RefreshAsync();
			await entranceAnimation;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(SettingsPage),
				nameof(viewModel.RefreshAsync),
				"Settings");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(SettingsPage), nameof(viewModel.RefreshAsync), exception);
			throw;
		}
	}

	private async void OnImportFromSmsClicked(object? sender, EventArgs e)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Requested Route={Route}",
			nameof(SettingsPage),
			nameof(OnImportFromSmsClicked),
			"SmsImport");

		try
		{
			await Shell.Current.GoToAsync("SmsImport");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(SettingsPage), nameof(OnImportFromSmsClicked), exception);
		}
	}

	private async void OnDeleteLocalDataClicked(object? sender, EventArgs e)
	{
		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Requested",
			nameof(SettingsPage),
			"PurgeLocalData");

		var confirmed = await DisplayAlertAsync(
			"Delete local data?",
			"This removes local app data from this device. This cannot be undone.",
			"Delete",
			"Cancel");

		if (!confirmed)
		{
			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Cancelled",
				nameof(SettingsPage),
				"PurgeLocalData");
			return;
		}

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Confirmed",
			nameof(SettingsPage),
			"PurgeLocalData");

		DeleteLocalDataButton.IsEnabled = false;
		try
		{
			await localDataPurgeService.PurgeAsync();
			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded",
				nameof(SettingsPage),
				"PurgeLocalData");
			await DisplayAlertAsync("Local data deleted", "Local app data was removed from this device.", "OK");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(SettingsPage), "PurgeLocalData", exception);
			await DisplayAlertAsync("Delete failed", "Local app data could not be removed. Try again.", "OK");
		}
		finally
		{
			DeleteLocalDataButton.IsEnabled = true;
		}
	}
}