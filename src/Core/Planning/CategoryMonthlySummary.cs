using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record CategoryMonthlySummary(string Category, MoneyAmount Expenses);