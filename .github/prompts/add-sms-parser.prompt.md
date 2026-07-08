---
description: "Add or adjust a deterministic SMS transaction parser from synthetic redacted examples."
argument-hint: "Paste synthetic redacted SMS examples and expected parsed fields"
agent: "agent"
---

Add or adjust SMS parsing for the provided synthetic redacted examples.

Requirements:
- Do not use real SMS content or personal identifiers.
- Keep parsing deterministic in `src/Core/Parsing`.
- Add MSTest coverage in `tests/Core.Tests`.
- Cover amount, currency, merchant, direction, source, reference when available, and null behavior for non-financial messages.
- Run `dotnet test tests\Core.Tests\Core.Tests.csproj` after changes.