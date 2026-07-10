---
description: "Review HasbeMaal changes for privacy, PII, secrets, logging, telemetry, and data-retention risks."
agent: "Privacy Security Reviewer"
---

Review the current changes for privacy and security risks.

Focus on:
- Real SMS content or personal finance data in code, docs, tests, logs, screenshots, or fixtures.
- Secrets, signing keys, connection strings, or exported databases.
- Raw SMS storage or logging.
- Telemetry containing PII, merchant names, exact amounts, account identifiers, or transaction trails.
- Cloud or AI features without explicit consent and data minimization.

Return findings first, ordered by severity, with file references and concrete fixes.