# Local MVP Threat Model

Last reviewed: 2026-07-09

This threat model covers the Local MVP only: Android-first local transaction capture, deterministic parsing, manual entries, encrypted local storage, monthly summaries, and category budgets. Cloud sync, cloud backup, telemetry, and AI insights are future/review-required features and must not be treated as in scope until they receive a separate privacy review.

## Current Implementation Status

- Core contains deterministic transaction parsing, money, duplicate detection, and planning primitives.
- Infrastructure contains `FileEncryptedStore` as an encrypted file storage primitive.
- App-level transaction persistence is not fully wired to the mobile experience yet.
- Android SMS ingestion and permission flows are not implemented yet.
- Raw SMS storage is not enabled and must remain disabled by default.

## Assets

- Structured transaction records: amounts, dates, directions, categories, source metadata, and notes.
- Raw source material: SMS body text, sender identifiers, source transaction references, account hints, UPI identifiers, and phone numbers.
- Local storage: encrypted files or databases, export files, backups, and purge state.
- Secrets: encryption keys, signing keys, API keys, tokens, and connection strings.
- App diagnostics: logs, crash reports, counters, parser outcomes, and validation failures.
- Source repository content: docs, tests, parser fixtures, issue text, screenshots, and generated artifacts.

## Data Classification

| Class | Data | Handling Rule |
| --- | --- | --- |
| Public | Source code, docs, parser rules, and synthetic redacted fixtures | May be committed when it contains no personal financial data. |
| Sensitive | Transaction details, raw SMS, sender identifiers, account hints, UPI identifiers, phone numbers, transaction references, exports, backups, and financial screenshots | Keep local by default, minimize retention, encrypt at rest, exclude from logs, and never commit. |
| Secret | Encryption keys, signing keys, API keys, tokens, connection strings, and recovery material | Store only in platform secret stores or developer secret stores. Never commit or log. |

## Data Flows

### Current

1. Tests and fixtures use synthetic redacted messages only.
2. Core parser code converts message-like input into structured parsed transaction results.
3. `FileEncryptedStore` can persist encrypted file content as an Infrastructure primitive.
4. Mobile UI uses placeholder pages and registered services, but does not yet provide a complete persisted transaction workflow.

### Local MVP Target

1. User grants Android SMS access explicitly.
2. Android platform code reads SMS content and passes only the required text to Core parser interfaces.
3. Core returns a structured transaction candidate or no match.
4. App services validate, deduplicate, and store structured transaction data through encrypted local persistence.
5. UI reads structured transactions and summaries from app services.
6. Delete and export controls operate on structured local data and any generated local artifacts.

Raw SMS, sender identifiers, and source references should not be stored after parsing unless a future diagnostic mode is explicitly reviewed, encrypted, time-limited, and purgeable.

### Future, Review-Required

- Cloud backup or sync.
- Telemetry or crash reporting.
- AI-generated insights.
- Raw-source diagnostics.
- Cross-device export, import, or restore flows.

## Trust Boundaries

- Device owner to HasbeMaal app: consent, local settings, manual entry, and delete/export actions.
- Android OS to Mobile platform code: SMS permission grant, SMS content access, and platform lifecycle events.
- Mobile app to Core: structured calls into deterministic parsing and domain logic.
- App services to Infrastructure: encrypted local persistence and future repository implementations.
- Local app storage to external readers: device backup tools, file browsers, malware, shared devices, and rooted devices.
- Repository to contributors and issue trackers: only synthetic fixtures and redacted documentation may cross this boundary.
- Any network or cloud boundary: out of Local MVP scope and review-required before implementation.

## Threats And Mitigations

| Threat | Risk | Required Mitigations |
| --- | --- | --- |
| Real financial data enters the repo, issues, screenshots, or fixtures | Personal financial exposure and permanent history retention | Use only synthetic redacted fixtures, review docs/tests before commit, and remove generated artifacts that may contain user data. |
| Raw SMS is stored by default | High-sensitivity source data remains on device longer than needed | Store structured fields only, keep raw-source diagnostics disabled by default, and require explicit privacy review for any raw retention. |
| Encrypted storage is bypassed or inconsistently used | Local transaction data may be readable from files or backups | Route real transaction persistence through encrypted storage, verify repository integration before release, and keep unencrypted test stores limited to synthetic data. |
| Keys or secrets are committed, logged, or stored with encrypted data | Encrypted content can be decrypted by unintended parties | Use platform key stores or developer secret stores, never commit keys, and exclude secrets from diagnostics. |
| Logs, telemetry, or crash reports include sensitive values | Sensitive data leaves the local trust boundary | Do not log raw SMS, sender identifiers, account hints, transaction references, merchant names from user data, UPI identifiers, phone numbers, or exact transaction trails. |
| SMS permission flow is unclear or too broad | Users may grant access without understanding local processing | Request permission only when needed, explain local processing in UI, support denial, and avoid background access beyond the Local MVP need. |
| Parser misclassification or duplicate failures corrupt summaries | Budgets and summaries become misleading | Keep parser deterministic, maintain synthetic coverage, preserve confidence scoring, and run focused Core tests after parser changes. |
| Delete, export, or purge flows miss local artifacts | User cannot reliably remove or control sensitive data | Track persisted artifacts, implement delete/export before public release, and include encrypted store files, indexes, and backups in purge tests. |
| Future cloud, telemetry, or AI features reuse Local MVP assumptions | Data crosses network boundaries without consent and retention controls | Treat all networked features as future/review-required and document consent, retention, deletion, export, failure modes, and sanitized payload rules before implementation. |

## Current Gaps

- App-level transaction persistence is not complete.
- Android SMS ingestion and permission UX are not implemented.
- Delete, export, and purge UX are not complete.
- Local encrypted persistence needs end-to-end repository integration tests before storing real transactions.
- Backup, telemetry, cloud sync, and AI insights do not have approved Local MVP data flows.

## Review Triggers

Update this threat model before any change that introduces or modifies:

- Android SMS permission, SMS ingestion, or sender/source handling.
- Transaction repository persistence, storage schema, encryption, key management, or migration logic.
- Raw-source diagnostics, debug exports, parser failure capture, or crash reporting.
- Delete, purge, export, import, backup, or restore flows.
- Telemetry, analytics, cloud sync, cloud backup, AI insights, or other network calls.
- Test fixtures, screenshots, docs, or issues that might include sensitive financial data.
- Dependency changes that affect storage, cryptography, logging, networking, or Android permissions.
