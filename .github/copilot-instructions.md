# Project Guidelines

## Architecture

- Keep finance domain logic in `src/Core`; it must not depend on MAUI, platform APIs, Azure SDKs, or storage SDKs.
- Keep platform integration and persistence in `src/Infrastructure` or MAUI platform folders behind interfaces.
- Use .NET MAUI Shell navigation, MVVM, and constructor injection for app code.
- Register shared services as singletons and Pages/ViewModels as transients in `MauiProgram.CreateMauiApp()`.

## Privacy And Security

- Never add real SMS messages, UPI IDs, phone numbers, account numbers, exported databases, screenshots with financial data, or secrets.
- Use synthetic redacted test fixtures only.
- Do not log raw SMS, exact transaction trails, account identifiers, merchant names from user data, or secrets.
- Raw SMS storage must be disabled by default. Any future raw-source diagnostic mode must be explicit, encrypted, and purgeable.

## Finance Rules

- Never use `float` or `double` for money. Use `decimal` or a dedicated money type.
- Parser logic must be deterministic and covered by tests.
- AI may assist with optional insights only after deterministic local logic works and only over sanitized summaries.

## AI Team Workflow

- Use `.github/agents/ai-team-lead.agent.md` for work that crosses product, architecture, engineering, QA, privacy, docs, or infrastructure.
- Default to the fast lane for one-lane, low-risk work; use full review only for cross-boundary, privacy/SMS/storage/cloud/telemetry/security, CI/release, public docs/security, or architecture decisions.
- Use at most one reviewer by default. Add reviewers only when the risk changes.
- Specialist agents must separate repo facts, assumptions, open questions, risks, and validation commands.
- Review-only agents should recommend validation, not run builds or tests unless explicitly asked. Implementing agents may run one focused validation.
- Keep AI-assisted development in small implementation slices and start a fresh chat or compact after each issue or large slice.
- Do not propose AI, cloud, telemetry, or raw-source diagnostics without explicit privacy review.

## Build And Test

- Run focused tests after domain changes: `dotnet test tests\Core.Tests\Core.Tests.csproj`.
- Run infrastructure persistence tests after persistence changes: `dotnet test tests\Infrastructure.Tests\Infrastructure.Tests.csproj`; run Core tests only if the Core contract changed.
- Run Mobile.Tests after presentation view model changes: `dotnet test tests\Mobile.Tests\Mobile.Tests.csproj`.
- Run Android build after MAUI changes: `dotnet build src\Mobile\Mobile.csproj -f net10.0-android`.
- For docs-only changes, use markdown diagnostics or a diff check only.
- Run the full suite only before release, after a broad cross-project change, or after fixing a failing CI/root cause.