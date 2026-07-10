---
description: "Review current HasbeMaal changes with the AI team before continuing, committing, or opening a pull request."
argument-hint: "Optional focus area, such as parser, mobile, privacy, CI, or docs"
agent: "AI Team Lead"
---

Review the current changes using the relevant HasbeMaal AI team roles.

Use one reviewer by default. Add more only when the change is cross-boundary, privacy/SMS/storage/cloud/telemetry/security-sensitive, CI/release-related, public docs/security-related, or architectural.

Review-only agents should not run builds or tests unless explicitly asked. Recommend validation from the focused budget instead.

Focus on:
- Behavioral regressions and missing tests.
- Privacy, PII, raw SMS, telemetry, AI, and cloud risks.
- Architecture boundary violations.
- CI/build/release path drift.
- Documentation that overstates implemented behavior.

Return findings first, then recommended fixes and validation commands.