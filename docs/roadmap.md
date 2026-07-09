# Roadmap

## Phase 1: Foundation

- Repository, docs, Copilot customizations, CI.
- Core money and transaction models.
- Deterministic SMS parser with synthetic fixtures.
- Placeholder MAUI navigation.

## Phase 2: Local MVP

- Manual transaction entry.
- [x] Android SMS permission consent flow.
- Encrypted local database.
- Transaction list and monthly summaries.
- Category budgets and cash entries.

## Phase 3: Planning Milestone

Planning is intentionally separate from the Local MVP. These features should build on deterministic local transaction and budget data rather than ship as part of the first product milestone.

- Emergency fund planning.
- Children education goals.
- Parents support planning.
- Recurring expenses and subscription detection.
- Scenario forecasting.

## Phase 4: Optional Cloud

- Opt-in encrypted backup.
- Optional sync API.
- PII-free telemetry.
- Sanitized AI insights.

## Phase 5: Public Readiness

- License selection.
- Privacy policy.
- [x] [Threat model](privacy/threat-model.md).
- [x] [Android SMS permission store review](privacy/android-sms-store-review.md).
- [x] GitHub private vulnerability reporting enabled.
- Accessibility and localization pass.
