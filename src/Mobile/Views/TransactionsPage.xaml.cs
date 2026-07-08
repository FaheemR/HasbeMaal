using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Views;

public partial class TransactionsPage : ContentPage
{
	private readonly TransactionsViewModel viewModel;

	public TransactionsPage(TransactionsViewModel viewModel)
	{
		this.viewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}
}