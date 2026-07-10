---
description: "Plan a small HasbeMaal implementation slice with product, architecture, engineering, QA, privacy, and infrastructure responsibilities."
argument-hint: "Describe the implementation slice"
agent: "AI Team Lead"
---

Plan the requested implementation slice with the AI team.

Use the fast lane unless the slice crosses project boundaries, changes privacy/SMS/storage/cloud/telemetry/security posture, affects CI/release, updates public docs/security guidance, or makes an architecture decision. Use at most one reviewer by default.

Include:
- Product outcome and non-goals.
- Architecture boundary and affected projects.
- Engineering tasks in order.
- Tests and the smallest validation command from the validation budget.
- Privacy/security risks and mitigations.
- Documentation or CI updates if needed.

Keep the slice small enough to implement and validate in one focused pass.
Review-only agents should recommend validation instead of running builds or tests unless explicitly asked. The implementing agent may run one focused validation.
Start a fresh chat or compact after each issue or large slice.