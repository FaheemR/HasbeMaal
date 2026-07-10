# HasbeMaal

HasbeMaal is an Android-first, local-first personal finance app for households that need private expense tracking, clear monthly budgeting, and planning workflows without sending personal finance data to the cloud by default.

The current product direction is the Local MVP: .NET MAUI Android support, deterministic local transaction logic, manual entries, encrypted local transaction storage, monthly summaries, category budgets, and optional Android SMS permission consent. Longer-term planning features such as emergency funds, education goals, family support, and scenario forecasting are a later milestone. iOS support is planned later through manual and import flows because iOS does not provide general SMS inbox access to apps.

## Current Status

- .NET 10 solution with Core, Infrastructure, Presentation, and Mobile projects.
- .NET MAUI Android app with bottom-tab navigation for Dashboard, Transactions, Add, Planning, and Settings.
- Premium mobile UI pass with route icons, stronger typography, elevated cards, glass-style panels, page entrance animations, loading states, and safe-area-aware pages.
- Manual transaction entry flow backed by `ITransactionApplicationService` and encrypted local persistence.
- Dashboard, transaction list, and budget progress views reading local structured transaction data.
- Android SMS permission consent flow in Settings. SMS ingestion is not implemented yet.
- Deterministic parser and domain logic covered by synthetic redacted tests.
- Privacy-safe local diagnostics for startup, Shell setup, page construction, navigation, view-model loading, parsing orchestration, persistence, transaction save/list flows, local data purge, and unhandled exceptions.
- Repeatable Android route screenshot capture for visual checks.

## Project Layout

- `src/Mobile` - .NET MAUI app
- `src/Presentation` - view models and UI-facing state
- `src/Core` - domain models, parser contracts, application service contracts, budgeting, and planning logic
- `src/Infrastructure` - encrypted local persistence and platform integration abstractions
- `tests/Core.Tests` - deterministic Core unit tests
- `tests/Infrastructure.Tests` - encrypted persistence and local purge tests
- `tests/Mobile.Tests` - presentation view model tests
- `docs` - product, architecture, privacy, contributing, roadmap, and AI operating notes
- `scripts/capture-android-route-screenshots.ps1` - Android route screenshot capture helper
- `.github/instructions` - Copilot task instructions
- `.github/agents` - role-based AI team agents
- `.github/prompts` - reusable Copilot prompts
- `.github/skills` - on-demand Copilot workflows
- `docs/ai` - AI team operating model and coordination rules

## Mobile App Experience

The Android app currently uses bottom-tab Shell navigation. The MAUI Android flyout renderer previously crashed during startup, so `FlyoutBehavior="Disabled"` is intentional until the flyout path is investigated and verified safe.

Current routes:

- Dashboard: executive monthly snapshot, cashflow position, empty-state actions, and local-first reminders.
- Transactions: grouped local ledger with premium empty state and quick Add action.
- Add: manual entry form with clean input chrome, delayed validation messaging, and save loading feedback.
- Planning: budget progress view with budget-framework empty affordances. Goals are present as a planning placeholder under the Planning tab.
- Settings: Android SMS permission consent, app-settings access, and local data purge controls.

## Privacy And Data Handling

HasbeMaal is local-first and privacy-first:

- Raw SMS storage is disabled by default.
- Manual transactions are stored as structured transaction data.
- Local persistence uses encrypted file storage primitives and platform-protected key storage.
- Android SMS access is optional and consent-driven.
- No cloud telemetry, analytics, crash upload, remote logging, AI analysis, backup, or sync is enabled in the Local MVP.

Do not commit real SMS messages, sender IDs, account numbers, UPI IDs, phone numbers, transaction references, merchant trails, bank names tied to personal activity, exported databases, backups, screenshots containing financial data, or secrets. Use synthetic redacted fixtures only.

See:

- [Data handling](docs/privacy/data-handling.md)
- [Local MVP threat model](docs/privacy/threat-model.md)
- [Android SMS store review notes](docs/privacy/android-sms-store-review.md)
- [Privacy-safe logging](docs/privacy/logging.md)

## Diagnostics

Debug diagnostics are local-only. In Android debug builds, `HasbeMaal.*` logs are emitted to logcat for startup and runtime diagnosis. Logs must use fixed operation/status fields and must not include raw SMS, sender IDs, account identifiers, UPI IDs, transaction references, source hashes, merchant names from user data, exact transaction amounts, storage paths, keys, secure storage values, serialized payloads, or transaction trails.

The logging policy is documented in [docs/privacy/logging.md](docs/privacy/logging.md).

## Build And Test

Prerequisites:

- .NET 10 SDK matching `global.json`
- .NET MAUI Android workload
- Android SDK/device or emulator for Android build and run validation

```powershell
dotnet restore HasbeMaal.slnx
dotnet test tests\Core.Tests\Core.Tests.csproj
dotnet test tests\Infrastructure.Tests\Infrastructure.Tests.csproj
dotnet test tests\Mobile.Tests\Mobile.Tests.csproj
dotnet build src\Mobile\Mobile.csproj -f net10.0-android
```

Run on a connected Android device:

```powershell
dotnet build src\Mobile\Mobile.csproj -f net10.0-android -t:Run
```

Capture route screenshots after deploying:

```powershell
.\scripts\capture-android-route-screenshots.ps1
```

Generated screenshots are written under `artifacts/android-screenshots`, which is ignored by git. Do not publish screenshots containing real personal financial data.

## Current Known Gaps

- SMS ingestion is not implemented yet; Settings currently covers permission consent and app-settings access.
- Shell flyout navigation remains disabled until the MAUI Android flyout renderer path is fixed and validated.
- Budget/category setup flows are not complete, so Planning currently shows empty-state affordances unless persisted categories exist.
- Goals are scaffolded as a planning surface, but persistence and create/edit workflows are future work.
- Visual checks are script-assisted screenshots, not CI-enforced visual regression tests.
- Public release still needs license selection, privacy policy, accessibility pass, localization review, and store-readiness work.

GitHub follow-up tracking: [issue #43](https://github.com/FaheemR/HasbeMaal/issues/43).

## License

No license has been selected yet. Until a `LICENSE` file is added, this public code is not licensed for reuse, redistribution, or derivative works.
