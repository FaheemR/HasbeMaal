using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record BudgetCategory
{
	public BudgetCategory(string name, MoneyAmount monthlyLimit, bool isEssential)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Category name is required.", nameof(name));
		}

		ArgumentNullException.ThrowIfNull(monthlyLimit);

		Name = name.Trim();
		MonthlyLimit = monthlyLimit;
		IsEssential = isEssential;
	}

	public string Name { get; }

	public MoneyAmount MonthlyLimit { get; }

	public bool IsEssential { get; }

	public MonthlyBudgetEvaluation EvaluateMonthlySpent(MoneyAmount spent)
	{
		ArgumentNullException.ThrowIfNull(spent);

		if (spent.Currency != MonthlyLimit.Currency)
		{
			throw new InvalidOperationException("Spent amount currency must match the category monthly limit currency.");
		}

		var remaining = Math.Max(MonthlyLimit.Amount - spent.Amount, 0m);
		var overspend = Math.Max(spent.Amount - MonthlyLimit.Amount, 0m);
		var percentUsed = MonthlyLimit.Amount switch
		{
			> 0m => spent.Amount / MonthlyLimit.Amount * 100m,
			0m when spent.Amount == 0m => 0m,
			_ => 100m
		};

		return new MonthlyBudgetEvaluation(
			Name,
			MonthlyLimit,
			spent,
			new MoneyAmount(remaining, MonthlyLimit.Currency),
			new MoneyAmount(overspend, MonthlyLimit.Currency),
			percentUsed,
			IsEssential);
	}

	public void Deconstruct(out string name, out MoneyAmount monthlyLimit, out bool isEssential)
	{
		name = Name;
		monthlyLimit = MonthlyLimit;
		isEssential = IsEssential;
	}
}

public sealed record MonthlyBudgetEvaluation(
	string CategoryName,
	MoneyAmount MonthlyLimit,
	MoneyAmount Spent,
	MoneyAmount Remaining,
	MoneyAmount Overspend,
	decimal PercentUsed,
	bool IsEssential);