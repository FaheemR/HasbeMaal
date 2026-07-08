# Architecture Overview

HasbeMaal is split into small projects so privacy-sensitive and test-heavy logic stays independent of platform UI code.

## Projects

- `Mobile`: .NET MAUI app, Shell navigation, Pages, ViewModels, platform permission flows.
- `Core`: deterministic domain logic, money types, transaction parsing, budgets, goals, forecasts.
- `Infrastructure`: persistence, backup, and platform integration abstractions and implementations. Encrypted persistence is a target capability, not an implemented storage layer yet.
- `Core.Tests`: parser, budgeting, forecasting, and model tests.

## Boundaries

- Core must not depend on MAUI, Android, iOS, Azure, or storage SDKs.
- Android SMS ingestion must call Core parser interfaces rather than embedding parsing rules in platform code.
- Infrastructure may depend on Core, but Core must not depend on Infrastructure.
- UI pages should use ViewModels and services through dependency injection.

## Current Scaffold

- The MAUI app currently provides placeholder navigation pages.
- Core currently contains domain models, parser contracts, deterministic parser scaffolding, and planning models.
- Infrastructure currently contains storage abstractions only; encrypted local persistence has not been implemented yet.
- Android SMS ingestion and permission flows have not been implemented yet.

## Target Local MVP Data Flow

1. Android receives or reads an SMS after explicit permission.
2. Platform service passes message text to a Core parser.
3. Core returns a structured parsed transaction or null.
4. Infrastructure stores structured data in encrypted local storage.
5. UI reads aggregates and transactions through application services.

Raw SMS storage must remain disabled by default. If a future diagnostic mode stores source text, sender IDs, or transaction references, it must be explicit, encrypted, and easy to purge.
