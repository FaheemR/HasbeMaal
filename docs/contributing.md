# Contributing

HasbeMaal welcomes contributions once the repository is public. Finance apps need a higher bar for privacy than ordinary apps.

## Rules

- Use synthetic redacted SMS examples only.
- Add parser tests for every new SMS format.
- Do not commit screenshots, databases, backups, logs, sender IDs, transaction references, or messages containing personal financial data.
- Keep Core logic deterministic and testable.
- Do not add cloud, AI, telemetry, or sync behavior without privacy documentation.
- Prefer small pull requests with focused tests.

## Parser Fixture Pattern

Use names like `REDACTED STORE`, `REDACTED SCHOOL`, `XX0000`, and `SYNTH001`. Do not paste real sender IDs, account suffixes, UPI IDs, or transaction references.

## Build Checks

Run these before submitting changes:

```powershell
dotnet test tests\Core.Tests\Core.Tests.csproj
dotnet build src\Mobile\Mobile.csproj -f net10.0-android
```
