# HasbeMaal

HasbeMaal is an Android-first, local-first personal finance app for households that need private expense tracking and clear monthly budgeting.

The first product milestone is the Local MVP: .NET MAUI Android support, deterministic SMS parsing, manual cash entries, local transaction storage, monthly summaries, and category budgets. Longer-term planning features such as emergency funds, education goals, family support, and scenario forecasting are a separate later milestone. iOS support is planned later through manual and import flows because iOS does not provide general SMS inbox access to apps.

## Current Status

- .NET 10 solution scaffold
- .NET MAUI mobile app with placeholder tabs
- Platform-independent Core library
- Deterministic SMS parser skeleton
- MSTest parser coverage with synthetic redacted messages
- Copilot instructions, prompts, and skills for safe future development

## Project Layout

- `src/Mobile` - .NET MAUI app
- `src/Core` - domain models, parser contracts, budgeting and planning logic
- `src/Infrastructure` - persistence and platform integration abstractions
- `tests/Core.Tests` - deterministic unit tests
- `docs` - product, architecture, privacy, contributing, and roadmap notes
- `.github/instructions` - Copilot task instructions
- `.github/agents` - role-based AI team agents
- `.github/prompts` - reusable Copilot prompts
- `.github/skills` - on-demand Copilot workflows
- `docs/ai` - AI team operating model and coordination rules

## Build And Test

```powershell
dotnet restore HasbeMaal.slnx
dotnet test tests\Core.Tests\Core.Tests.csproj
dotnet build src\Mobile\Mobile.csproj -f net10.0-android
```

## Privacy Rule

Do not commit real SMS messages, sender IDs, account numbers, UPI IDs, phone numbers, transaction references, merchant trails, bank names tied to personal activity, exported databases, backups, screenshots containing financial data, or secrets. Use synthetic redacted fixtures only.

See [data handling](docs/privacy/data-handling.md), the [Local MVP threat model](docs/privacy/threat-model.md), and the [Android SMS store review notes](docs/privacy/android-sms-store-review.md) for contributor-facing privacy boundaries.

## License

No license has been selected yet. Until a `LICENSE` file is added, this public code is not licensed for reuse, redistribution, or derivative works.
