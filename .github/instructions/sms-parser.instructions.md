---
description: "Use when adding or modifying SMS transaction parsers, UPI parsing, GPay parsing, bank SMS parsing, credit card SMS parsing, wallet parsing, duplicate detection, or parser fixtures."
applyTo: ["src/Core/Parsing/**/*.cs", "tests/Core.Tests/**/*.cs"]
---

# SMS Parser Guidelines

- Parser behavior must be deterministic and test-covered.
- Add at least one synthetic redacted fixture per new SMS format.
- Do not paste real bank SMS messages. Rewrite them with synthetic amounts, references, sender details, and merchants.
- Return null for non-financial messages.
- Include confidence and source classification when possible.
- Keep duplicate detection based on hashes or normalized synthetic references, not raw message text.