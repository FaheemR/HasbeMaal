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

## Procedure

1. Frame the user's requested outcome in one paragraph.
2. Identify the smallest evidence needed from existing docs, code, tests, or workflows.
3. Delegate only the relevant questions to specialist agents.
4. Ask each specialist to separate facts, assumptions, risks, and open questions.
5. Convert findings into one implementation slice with one validation command.
6. Stop or ask the user only when a decision changes scope, privacy posture, cost, or cloud usage.

## Hallucination Controls

- Require file evidence for repo claims.
- Treat missing code as missing, not planned or implied.
- Do not invent SMS formats, platform permissions, user finances, Azure resources, or policy approvals.
- Keep AI features opt-in and limited to sanitized summaries until deterministic local logic works.

## Output Template

```markdown
**Scope**
<one paragraph>

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