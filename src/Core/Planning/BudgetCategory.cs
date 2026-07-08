using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record BudgetCategory(string Name, MoneyAmount MonthlyLimit, bool IsEssential);