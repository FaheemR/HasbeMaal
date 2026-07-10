using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Views;

public partial class DashboardPage : ContentPage
{
	private readonly DashboardViewModel viewModel;

	public DashboardPage(DashboardViewModel viewModel)
	{
		this.viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		await viewModel.LoadAsync();
	}
}