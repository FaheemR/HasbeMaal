# AI Team Operating Model

HasbeMaal uses repo-local Copilot customizations to make AI assistance role-based and bounded. The goal is to use AI as a focused project team without letting broad prompts drift into unsafe assumptions.

## Team Roles

- Product Strategist: user outcomes, scenarios, non-goals, acceptance criteria, roadmap priority.
- Solution Architect: project boundaries, data flow, dependency direction, tradeoffs.
- Core Engineer: deterministic finance logic, money handling, SMS parser rules, Core tests.
- Mobile Engineer: .NET MAUI pages, Shell navigation, dependency injection, platform adapters.
- QA Engineer: test strategy, MSTest coverage, edge cases, regression checks.
- Privacy Security Reviewer: PII, SMS data, consent, telemetry, AI/cloud risk, retention.
- Infrastructure Engineer: CI/CD, GitHub Actions, release artifacts, optional Azure planning.
- Technical Writer: docs, contribution guidance, architecture notes, handoffs.

## Coordination Rules

Use the fast lane by default for one-lane, low-risk work. Use the `AI Team Lead` agent when a task crosses more than one role or needs full review.

Full review is reserved for cross-boundary changes, privacy/SMS/storage/cloud/telemetry/security work, CI/release changes, public docs/security updates, or architecture decisions. Use one reviewer by default and add more only when the risk changes.

Review-only agents recommend validation instead of running builds or tests unless explicitly asked. The implementing agent may run one focused validation.

Each agent should report:

- Evidence consulted.
- Facts found in the repo.
- Assumptions and open questions.
- Risks and mitigations.
- Smallest next step and validation command.

Agents cannot directly chat with each other. For council-style review, the AI Team Lead asks Agent A for facts and risks, passes distilled findings to Agent B for response, then reconciles the decision. Examples: product and engineering align scope before implementation; architecture and infrastructure align deployment boundaries; privacy and mobile align SMS permission and consent behavior.

Start a fresh chat or compact after each issue or large slice. Avoid mixing issue implementation, CI triage, and UI design in one long session when separate sessions would keep context clearer.

## New Thread Checklist

Use this checklist at the start of a fresh thread, after compaction, or before continuing issue work:

- Check the current GitHub issue or open issue queue with GitHub or GitKraken tools.
- Check branch, worktree, commit, push, and remote-sync state before assuming work is complete.
- State the target issue, commit intent, push intent, focused validation command, and stop condition.
- Read only the smallest repo evidence needed to make the next slice falsifiable.
- Use the questions tool for structured user decisions about scope, privacy posture, cloud usage, commit, or push.
- Work one issue or slice at a time, validate it, update the issue when requested, then hand off or compact.
- Treat README and the issue tracker as current-state sources; use architecture docs for boundaries and cross-check stale assumptions.

## Validation Budget

- Core-only changes: `dotnet test tests\Core.Tests\Core.Tests.csproj`.
- Infrastructure persistence changes: `dotnet test tests\Infrastructure.Tests\Infrastructure.Tests.csproj`; add Core tests only if a Core contract changed.
- Presentation view model changes: `dotnet test tests\Mobile.Tests\Mobile.Tests.csproj`.
- MAUI XAML or page changes: `dotnet build src\Mobile\Mobile.csproj -f net10.0-android`; add tests only if a view model changed.
- Docs-only changes: markdown diagnostics or diff check only.
- Full suite: before release, after broad cross-project changes, or after fixing a failing CI/root cause.

## Safety Rules

- Do not use real SMS text, account identifiers, UPI IDs, phone numbers, transaction references, exports, backups, or screenshots.
- Keep parsing and budgeting deterministic before using AI for optional insights.
- Keep AI insights opt-in and based on sanitized summaries only.
- Do not provision Azure or cloud resources unless explicitly requested and privacy reviewed.
- Prefer one implementation slice at a time with the focused validation budget above.
