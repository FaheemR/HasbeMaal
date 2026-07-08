---
description: "Use when working on money handling, transactions, budgets, categories, goals, forecasts, family financial planning, or strategic scenarios."
applyTo: ["src/Core/Domain/**/*.cs", "src/Core/Planning/**/*.cs", "tests/Core.Tests/**/*.cs"]
---

# Budget Domain Guidelines

- Use `MoneyAmount` or `decimal` for money. Never use `float` or `double`.
- Keep calculations deterministic and unit-tested.
- Separate essentials, discretionary spending, savings, debt, family support, and long-term goals.
- Prefer explainable rules before AI-generated recommendations.
- Scenario planning should show assumptions clearly and avoid pretending to be financial advice.