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

Use the `AI Team Lead` agent when a task crosses more than one role. Use a specialist directly only when the task clearly belongs to one lane.

Each agent should report:

- Evidence consulted.
- Facts found in the repo.
- Assumptions and open questions.
- Risks and mitigations.
- Smallest next step and validation command.

## Safety Rules

- Do not use real SMS text, account identifiers, UPI IDs, phone numbers, transaction references, exports, backups, or screenshots.
- Keep parsing and budgeting deterministic before using AI for optional insights.
- Keep AI insights opt-in and based on sanitized summaries only.
- Do not provision Azure or cloud resources unless explicitly requested and privacy reviewed.
- Prefer one implementation slice at a time, validated by `dotnet test tests\Core.Tests\Core.Tests.csproj` or `dotnet build src\Mobile\Mobile.csproj -f net10.0-android` as appropriate.
