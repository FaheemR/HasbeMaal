---
name: ai-team-workflow
description: "Use when coordinating HasbeMaal AI agents, subagents, product team, engineering team, QA, privacy, infrastructure, roadmap planning, or multi-role implementation handoffs."
argument-hint: "Describe the feature, decision, or implementation slice to coordinate"
---

# AI Team Workflow

Use this workflow to coordinate role-specific agents without losing focus or inventing requirements.

## When To Use

- Planning a new feature or implementation slice.
- Turning a broad idea into product, architecture, tests, and delivery steps.
- Reviewing whether a proposed AI, cloud, SMS, storage, or telemetry feature is safe.
- Preparing a handoff between product, engineering, QA, privacy, docs, and infrastructure.

## Lane Selection

- Fast lane is the default for one-lane, low-risk tasks.
- Full review is only for cross-boundary, privacy/SMS/storage/cloud/telemetry/security, CI/release, public docs/security, or architecture decisions.
- Use one reviewer by default. Add reviewers only when risk changes.
- Review-only agents recommend validation and do not run builds or tests unless explicitly asked.
- The implementing agent may run one focused validation.

## Procedure

1. Frame the user's requested outcome in one paragraph.
2. Identify the smallest evidence needed from existing docs, code, tests, or workflows.
3. Choose fast lane or full review.
4. Delegate only the relevant questions to specialist agents.
5. For council-style review, ask Agent A for facts and risks, pass distilled findings to Agent B, then reconcile decisions.
6. Ask each specialist to separate facts, assumptions, risks, and open questions.
7. Convert findings into one implementation slice with one focused validation.
8. Start a fresh chat or compact after each issue or large slice.
9. Stop or ask the user only when a decision changes scope, privacy posture, cost, or cloud usage.

## Agent Council Examples

- Product to engineering: confirm the user outcome and non-goals, then translate into an implementation slice.
- Architecture to infrastructure: confirm boundaries, hosting, CI, and deployment impact before infrastructure work.
- Privacy to mobile: confirm SMS permission, consent, retention, and UI disclosure before platform changes.

## Validation Budget

- Core-only: `dotnet test tests\Core.Tests\Core.Tests.csproj`.
- Infrastructure persistence: `dotnet test tests\Infrastructure.Tests\Infrastructure.Tests.csproj`; add Core tests only if a Core contract changed.
- Presentation view model: `dotnet test tests\Mobile.Tests\Mobile.Tests.csproj`.
- MAUI XAML or page: `dotnet build src\Mobile\Mobile.csproj -f net10.0-android`; add tests only if a view model changed.
- Docs-only: markdown diagnostics or diff check only.
- Full suite: before release, after broad cross-project changes, or after fixing a failing CI/root cause.

## Hallucination Controls

- Require file evidence for repo claims.
- Treat missing code as missing, not planned or implied.
- Do not invent SMS formats, platform permissions, user finances, Azure resources, or policy approvals.
- Keep AI features opt-in and limited to sanitized summaries until deterministic local logic works.

## Output Template

```markdown
**Scope**
<one paragraph>

**Lane**
Fast lane or full review, with reason.

**Evidence**
- <docs/code/tests consulted>

**Team Findings**
- Product: <finding>
- Architecture: <finding>
- Engineering: <finding>
- QA: <finding>
- Privacy: <finding>
- Infrastructure: <finding>

**Next Slice**
<smallest useful implementation step>

**Validation**
`<command>`
```