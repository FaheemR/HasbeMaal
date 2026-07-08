---
name: budget-planner
description: "Use when designing or implementing HasbeMaal budgets, monthly categories, emergency fund planning, family support planning, education goals, long-term goals, recurring expenses, subscriptions, forecasts, and strategic what-if scenarios."
---

# Budget Planner Workflow

## Procedure

1. Clarify the household outcome and time horizon.
2. Separate essentials, discretionary spending, savings, debt, family support, and goals.
3. Represent assumptions explicitly.
4. Keep calculations deterministic in the `Core` project.
5. Add tests for edge cases, monthly projections, and goal progress.
6. Present outputs as planning support, not financial advice.

## Modeling Notes

- Use `MoneyAmount` or `decimal` for money.
- Keep date and recurrence logic explicit.
- Prefer transparent rules before AI suggestions.