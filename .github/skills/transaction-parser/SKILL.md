---
name: transaction-parser
description: "Use when building, reviewing, or debugging HasbeMaal SMS transaction parsing, GPay UPI parsing, bank SMS parsing, credit card SMS parsing, wallet parsing, parser confidence, duplicate detection, and redacted parser fixtures."
---

# Transaction Parser Workflow

## When To Use

- Adding a new SMS format.
- Fixing parser false positives or false negatives.
- Improving duplicate detection.
- Reviewing parser fixture quality.

## Procedure

1. Confirm every example is synthetic and redacted.
2. Add or update parser rules in `src/Core/Parsing`.
3. Add MSTest cases in `tests/Core.Tests`.
4. Include non-financial negative examples.
5. Keep parser output deterministic and explainable.
6. Run `dotnet test tests\Core.Tests\Core.Tests.csproj`.

## Privacy Rules

- Do not store raw SMS in fixtures.
- Do not include real sender IDs, account suffixes, UPI IDs, phone numbers, or transaction references.
- Use placeholders such as `REDACTED STORE`, `XX0000`, and `SYNTH001`.