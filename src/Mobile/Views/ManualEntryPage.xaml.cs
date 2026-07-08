using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Views;

public partial class ManualEntryPage : ContentPage
{
	public ManualEntryPage(ManualTransactionEntryViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}