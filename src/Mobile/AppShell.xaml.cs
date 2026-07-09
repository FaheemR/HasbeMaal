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
		TransactionsShellContent.ContentTemplate = new DataTemplate(() =>
			serviceProvider.GetRequiredService<TransactionsPage>());
		ManualEntryShellContent.ContentTemplate = new DataTemplate(() =>
			serviceProvider.GetRequiredService<ManualEntryPage>());
		BudgetsShellContent.ContentTemplate = new DataTemplate(() =>
			serviceProvider.GetRequiredService<BudgetsPage>());
		SettingsShellContent.ContentTemplate = new DataTemplate(() =>
			serviceProvider.GetRequiredService<SettingsPage>());
	}
}
