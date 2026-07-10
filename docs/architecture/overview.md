# Architecture Overview

HasbeMaal is split into small projects so privacy-sensitive and test-heavy logic stays independent of platform UI code.

## Projects

- `Mobile`: .NET MAUI Android app, Shell navigation, Pages, platform permission flows, local diagnostics, and Android route screenshot automation.
- `Presentation`: UI-facing ViewModels, commands, loading state, validation state, and screen composition logic.
- `Core`: deterministic domain logic, money types, transaction parsing, budgets, goals, forecasts.
- `Infrastructure`: encrypted local persistence, repository implementations, local purge services, and platform-protected key provider abstractions.
- `Core.Tests`: parser, budgeting, forecasting, and model tests.
- `Infrastructure.Tests`: encrypted store, repository, key-provider, and local purge tests.
- `Mobile.Tests`: presentation ViewModel tests.

## Boundaries

- Core must not depend on MAUI, Android, iOS, Azure, or storage SDKs.
- Android SMS ingestion must call Core parser interfaces rather than embedding parsing rules in platform code.
- Infrastructure may depend on Core, but Core must not depend on Infrastructure.
- Presentation may depend on Core application contracts and must stay platform-neutral.
- UI pages should use ViewModels and services through dependency injection.
- Cloud, AI, telemetry, backup, and sync remain optional future work and require privacy review before implementation.

## Current Local MVP State

- The MAUI Android app uses bottom-tab Shell navigation for Dashboard, Transactions, Add, Planning, and Settings.
- The Shell flyout is intentionally disabled because the MAUI Android flyout renderer was the observed startup crash path. Keep it disabled until a safe restoration is investigated and validated.
- Manual transaction entry is wired through `ITransactionApplicationService` and encrypted local transaction persistence.
- Dashboard, transaction list, and budget progress surfaces read structured local data through application services and repositories.
- Core contains deterministic domain models, parser contracts and implementation, duplicate detection, monthly summaries, budget category rules, recurring transaction detection, scenario assumptions, and goal contribution projections.
- Infrastructure contains encrypted file storage, transaction and budget category repositories, platform key provider abstractions, and local data purge support.
- Android SMS permission consent is available from Settings. SMS ingestion is not implemented yet.
- Debug diagnostics are local-only and privacy-safe; no cloud telemetry, analytics, crash upload, remote logging, AI analysis, backup, or sync is enabled in the Local MVP.

## Target Local MVP Data Flow

1. Android receives or reads an SMS after explicit permission.
2. Platform service passes message text to a Core parser.
3. Core returns a structured parsed transaction or null.
4. Infrastructure stores structured data in encrypted local storage.
5. UI reads aggregates and transactions through application services.

Raw SMS storage must remain disabled by default. If a future diagnostic mode stores source text, sender IDs, or transaction references, it must be explicit, encrypted, and easy to purge.

## Current Known Gaps

- SMS ingestion is still pending; consent and settings access exist first.
- Budget/category setup flows are not complete, so Planning can still show empty-state affordances.
- Goals have planning surface support, but persistence and create/edit flows are future work.
- Visual checks use local route screenshots rather than CI-enforced visual regression tests.
- Public readiness still needs license selection, privacy policy, accessibility review, localization review, and store-readiness work.

See the [Local MVP threat model](../privacy/threat-model.md) for privacy assets, trust boundaries, current gaps, and review triggers.
