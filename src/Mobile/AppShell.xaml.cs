using HasbeMaal.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HasbeMaal.Mobile;

public partial class AppShell : Shell
{
	private readonly IServiceProvider serviceProvider;

	public AppShell(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
		InitializeComponent();
		ManualEntryShellContent.ContentTemplate = new DataTemplate(() =>
			serviceProvider.GetRequiredService<ManualEntryPage>());
		SettingsShellContent.ContentTemplate = new DataTemplate(() =>
			serviceProvider.GetRequiredService<SettingsPage>());
	}
}
