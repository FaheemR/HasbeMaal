---
description: "Work the next open HasbeMaal issue as one focused implementation slice."
argument-hint: "Optional focus area, issue number, or validation preference"
agent: "AI Team Lead"
---

Work the next open HasbeMaal issue as a single focused slice.

Requirements:
- Use GitHub or GitKraken tools to inspect open issues and pick the next actionable issue, unless the user named a specific issue.
- State the target issue, current branch/worktree status, commit intent, push intent, validation command, and stop condition before implementation.
- Use the questions tool if the issue choice, scope, privacy posture, cloud usage, commit intent, or push intent needs a user decision.
- Use the fast lane unless the issue crosses project boundaries, changes privacy/SMS/storage/cloud/telemetry/security posture, affects CI/release, updates public docs/security guidance, or makes an architecture decision.
- Implement only the target issue slice. Do not opportunistically fix unrelated issues.
- Use the smallest validation command from the validation budget.
- Do not commit, push, or close the issue unless explicitly requested.

Return:
1. Issue-first state.
2. Evidence consulted.
3. Facts, assumptions, risks, and open questions.
4. Files changed and validation run.
5. Commit/push/issue update status.
6. Handoff or next recommended slice.