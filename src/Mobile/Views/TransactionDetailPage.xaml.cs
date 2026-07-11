using System.Globalization;
using HasbeMaal.Mobile.Services;
using HasbeMaal.Presentation.ViewModels;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class TransactionDetailPage : ContentPage, IQueryAttributable
{
	private readonly TransactionDetailViewModel viewModel;
	private readonly ILogger<TransactionDetailPage> logger;
	private Guid transactionId;

	public TransactionDetailPage(
		TransactionDetailViewModel viewModel,
		ILogger<TransactionDetailPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(TransactionDetailPage),
			nameof(InitializeComponent),
			"TransactionDetail");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(TransactionDetailPage),
				nameof(InitializeComponent),
				"TransactionDetail");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(TransactionDetailPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		ArgumentNullException.ThrowIfNull(query);

		if (query.TryGetValue("id", out var value)
			&& Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var id))
		{
			transactionId = id;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		try
		{
			if (transactionId != Guid.Empty)
			{
				await viewModel.LoadAsync(transactionId);
			}

			await entranceAnimation;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(TransactionDetailPage), nameof(viewModel.LoadAsync), exception);
			throw;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(TransactionDetailPage), nameof(OnBackClicked), exception);
		}
	}
}
