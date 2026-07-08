using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Mobile.Views;

public partial class SettingsPage : ContentPage
{
	private readonly ILocalDataPurgeService localDataPurgeService;

	public SettingsPage(ILocalDataPurgeService localDataPurgeService)
	{
		this.localDataPurgeService = localDataPurgeService;
		InitializeComponent();
	}

	private async void OnDeleteLocalDataClicked(object? sender, EventArgs e)
	{
		var confirmed = await DisplayAlertAsync(
			"Delete local data?",
			"This removes local app data from this device. This cannot be undone.",
			"Delete",
			"Cancel");

		if (!confirmed)
		{
			return;
		}

		DeleteLocalDataButton.IsEnabled = false;
		try
		{
			await localDataPurgeService.PurgeAsync();
			await DisplayAlertAsync("Local data deleted", "Local app data was removed from this device.", "OK");
		}
		catch (Exception)
		{
			await DisplayAlertAsync("Delete failed", "Local app data could not be removed. Try again.", "OK");
		}
		finally
		{
			DeleteLocalDataButton.IsEnabled = true;
		}
	}
}