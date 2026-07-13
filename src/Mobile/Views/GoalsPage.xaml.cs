using System.Globalization;
using HasbeMaal.Mobile.Services;
using HasbeMaal.Presentation.ViewModels;
using Microsoft.Extensions.Logging;

namespace HasbeMaal.Mobile.Views;

public partial class GoalsPage : ContentPage
{
	private readonly GoalsViewModel viewModel;
	private readonly ILogger<GoalsPage> logger;

	public GoalsPage(GoalsViewModel viewModel, ILogger<GoalsPage> logger)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		ArgumentNullException.ThrowIfNull(logger);

		this.viewModel = viewModel;
		this.logger = logger;

		logger.LogDebug(
			"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Started Route={Route}",
			nameof(GoalsPage),
			nameof(InitializeComponent),
			"Goals");

		try
		{
			InitializeComponent();
			BindingContext = viewModel;
			EmergencyTargetDatePicker.Date = DateTime.Today.AddMonths(12);

			logger.LogDebug(
				"HasbeMaal diagnostic Component={Component} Operation={Operation} Status=Succeeded Route={Route}",
				nameof(GoalsPage),
				nameof(InitializeComponent),
				"Goals");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(GoalsPage), nameof(InitializeComponent), exception);
			throw;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var entranceAnimation = this.AnimateEntranceAsync();

		try
		{
			await viewModel.LoadAsync();
			await entranceAnimation;
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(GoalsPage), nameof(viewModel.LoadAsync), exception);
		}
	}

	private async void OnDeleteGoalClicked(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: GoalItemViewModel item })
		{
			return;
		}

		try
		{
			await viewModel.DeleteGoalAsync(item.Id);
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(GoalsPage), nameof(OnDeleteGoalClicked), exception);
		}
	}

	private async void OnAddEmergencyFundClicked(object? sender, EventArgs e)
	{
		if (!TryReadDecimal(EmergencyEssentialsEntry.Text, out var essentials) || essentials <= 0m)
		{
			ShowEmergencyStatus("Enter your essential monthly expenses.");
			return;
		}

		if (!int.TryParse(EmergencyMonthsEntry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var months)
			|| months <= 0)
		{
			ShowEmergencyStatus("Enter how many months of cover you want.");
			return;
		}

		var currentSaved = 0m;
		if (!string.IsNullOrWhiteSpace(EmergencyCurrentEntry.Text)
			&& (!TryReadDecimal(EmergencyCurrentEntry.Text, out currentSaved) || currentSaved < 0m))
		{
			ShowEmergencyStatus("Enter a valid saved amount, or leave it blank.");
			return;
		}

		try
		{
			await viewModel.AddEmergencyFundAsync(
				essentials,
				months,
				currentSaved,
				DateOnly.FromDateTime((EmergencyTargetDatePicker.Date ?? DateTime.Today).Date));

			EmergencyEssentialsEntry.Text = string.Empty;
			EmergencyCurrentEntry.Text = string.Empty;
			ShowEmergencyStatus("Emergency fund added.");
		}
		catch (Exception exception)
		{
			logger.LogSanitizedException(nameof(GoalsPage), nameof(OnAddEmergencyFundClicked), exception);
			ShowEmergencyStatus("Could not add emergency fund. Try again.");
		}
	}

	private void ShowEmergencyStatus(string message)
	{
		EmergencyStatusLabel.Text = message;
		EmergencyStatusBorder.IsVisible = true;
	}

	private static bool TryReadDecimal(string? value, out decimal result)
	{
		return decimal.TryParse(
			value,
			NumberStyles.AllowLeadingWhite
				| NumberStyles.AllowTrailingWhite
				| NumberStyles.AllowThousands
				| NumberStyles.AllowDecimalPoint,
			CultureInfo.InvariantCulture,
			out result);
	}
}
