using HasbeMaal.Core.Domain;

namespace HasbeMaal.Core.Planning;

public sealed record FinancialGoal(
    string Name,
    MoneyAmount TargetAmount,
    MoneyAmount CurrentAmount,
    DateOnly TargetDate,
    string Purpose);