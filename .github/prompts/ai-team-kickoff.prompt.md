---
description: "Coordinate the HasbeMaal AI team for a feature, decision, roadmap item, or implementation slice."
argument-hint: "Describe the feature or decision to coordinate"
agent: "AI Team Lead"
---

Use the HasbeMaal AI team workflow for the requested work.

Requirements:
- Start from repo evidence, not assumptions.
- Use the fast lane for one-lane, low-risk work.
- Use full review only for cross-boundary, privacy/SMS/storage/cloud/telemetry/security, CI/release, public docs/security, or architecture decisions.
- Use at most one reviewer by default; add reviewers only when risk changes.
- Delegate to only the specialist roles needed.
- Mediate any agent-to-agent council through the AI Team Lead by distilling one agent's facts and risks before asking another to respond.
- Separate facts, assumptions, risks, and open questions.
- End with one focused next slice and the smallest validation from the validation budget.
- For docs-only work, recommend markdown diagnostics or diff check only.
- Start a fresh chat or compact after each issue or large slice.
- Do not propose cloud, AI, telemetry, or raw SMS handling without privacy review.