using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Views;

public partial class BudgetsPage : ContentPage
{
	private readonly BudgetsViewModel viewModel;

	public BudgetsPage(BudgetsViewModel viewModel)
	{
		this.viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}
}