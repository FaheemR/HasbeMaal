---
description: "Use when working on local encrypted persistence, file stores, repositories, purge flows, platform key providers, or Infrastructure persistence tests."
applyTo: ["src/Infrastructure/Persistence/**/*.cs", "tests/Infrastructure.Tests/**/*.cs"]
---

# Local Persistence Guidelines

- Keep the Local MVP offline-first and encrypted by default.
- Store structured transaction, budget, and planning data; do not store raw SMS, sender IDs, account identifiers, UPI IDs, phone numbers, transaction references, or merchant trails from user data.
- Keep persistence behind Core interfaces; Core must not depend on Infrastructure or platform APIs.
- Use platform-protected key storage through interfaces and avoid logging keys, storage paths, serialized payloads, raw exception objects, or transaction trails.
- Local purge flows must be explicit, test-covered, and safe to rerun.
- Run `dotnet test tests\Infrastructure.Tests\Infrastructure.Tests.csproj` after persistence changes; add Core tests only if a Core contract changed.