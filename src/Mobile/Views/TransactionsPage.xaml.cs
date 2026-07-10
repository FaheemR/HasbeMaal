using HasbeMaal.Presentation.ViewModels;
using HasbeMaal.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class TransactionsPage : ContentPage
{
	private readonly TransactionsViewModel viewModel;
	private readonly ILogger<TransactionsPage> logger;

	public TransactionsPage(
		TransactionsViewModel viewModel,
		ILogger<TransactionsPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(TransactionsPage),
			nameof(InitializeComponent),
			"Transactions");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(TransactionsPage),
				nameof(InitializeComponent),
				"Transactions");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(TransactionsPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(TransactionsPage),
			nameof(viewModel.LoadAsync),
			"Transactions");

		try
		{
			await viewModel.LoadAsync();
			await entranceAnimation;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(TransactionsPage),
				nameof(viewModel.LoadAsync),
				"Transactions");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(TransactionsPage), nameof(viewModel.LoadAsync), exception);
			throw;
		}
	}

	private async void OnAddEntryClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//ManualEntry");
	}
}
