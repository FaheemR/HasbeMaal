# Product Vision

HasbeMaal helps households track daily spending, understand monthly pressure, and prepare for future obligations without surrendering private financial data by default.

## Primary User

The first user is a household budget owner who manages shared expenses, recurring obligations, dependents, savings targets, and large upcoming costs. The app should support daily discipline first, then later add planning tools for goals, emergency funds, family support, and future scenarios.

## Local MVP Outcomes

- Capture GPay/UPI, bank, credit card, wallet, and manual cash transactions.
- Parse SMS messages deterministically with redacted test coverage.
- Show monthly spending by category.
- Track essentials versus discretionary spend.
- Keep data local by default, with encrypted persistence required before storing real transactions.

## Later Planning Outcomes

- Plan emergency fund and long-term goals.
- Model recurring obligations and future costs.
- Show scenario assumptions clearly without presenting recommendations as financial advice.

## Non-Goals For Local MVP

- Automated investment advice.
- Bank aggregation APIs.
- Public cloud sync by default.
- iOS SMS inbox automation.
- AI-based parsing as the primary parser.
