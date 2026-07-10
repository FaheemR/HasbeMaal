---
description: "Run a mediated HasbeMaal agent council for a focused decision or risk review."
argument-hint: "Describe the decision, risk, or disagreement to reconcile"
agent: "AI Team Lead"
---

Run a mediated agent council for the requested decision.

Rules:
- Agents cannot directly chat with each other; the AI Team Lead mediates.
- Ask Agent A for facts, risks, and open questions.
- Pass distilled findings to Agent B for response.
- Reconcile the decision, assumptions, and next step.
- Use product to engineering, architecture to infrastructure, or privacy to mobile pairings when they match the risk.
- Keep review to one reviewer unless the risk changes.
- Review-only agents recommend validation instead of running builds or tests unless explicitly asked.
- Start a fresh chat or compact after each issue or large slice.

Return:
1. Decision or question.
2. Agents consulted and why.
3. Distilled findings and response.
4. Reconciled decision, risks, and open questions.
5. Smallest validation from the validation budget.