# Local MVP Threat Model

Last reviewed: 2026-07-11

This threat model covers the Local MVP only: Android-first local transaction capture, deterministic parsing, on-device SMS import, manual entries, encrypted local storage, monthly summaries, and category budgets. Cloud sync, cloud backup, telemetry, and AI insights are future/review-required features and must not be treated as in scope until they receive a separate privacy review.

For Android `READ_SMS` permission purpose, consent copy, denial/revocation behavior, and store-review data constraints, see the [Android SMS store review notes](android-sms-store-review.md).

## Current Implementation Status

- Core contains deterministic transaction parsing, money, duplicate detection, and planning primitives.
- Infrastructure contains `FileEncryptedStore` as an encrypted file storage primitive.
- App-level transaction persistence is not fully wired to the mobile experience yet.
- Android SMS permission consent is implemented in Settings. On-device SMS import is approved under this reviewed design and is being implemented as a user-initiated, foreground, allowlist-filtered flow. See the [Android SMS store review notes](android-sms-store-review.md).
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

1. User grants Android SMS access explicitly, then starts import from the app foreground. There is no background SMS monitoring.
2. Android platform code reads the SMS inbox (full inbox on the first import, then only messages newer than the stored watermark) and applies a sender allowlist. The allowlist is seeded with common bank and UPI sender IDs and can be extended or reduced by the user. The sender address is used only for allowlist filtering and is dropped before any content leaves the platform boundary.
3. For each allowlisted message, Android passes only the message body and its received timestamp to Core parser interfaces. Sender identifiers, phone numbers, and account hints are never passed to Core.
4. Core parses each message deterministically and returns a structured transaction candidate with a confidence level, or no match. The received timestamp becomes the transaction date. A source reference (for example a UPI reference number), when present, is both reduced to a one-way hash for duplicate detection and retained as a raw value on the transaction so it can be shown to the user. The raw reference is encrypted at rest with all other transaction fields, is never written to logs or telemetry, and is removed by the local delete/purge flow.
5. App services deduplicate the batch against existing transactions and within the batch using two keys: the hashed source reference and a composite key of amount, direction, received timestamp (minute granularity), and normalized merchant. Duplicates are skipped.
6. High-confidence, non-duplicate candidates are committed to encrypted local persistence in a single user action. Lower-confidence candidates are surfaced for grouped, bulk review before they are committed or discarded; un-actioned candidates remain pending for a later import, subject to a cap.
7. After a successful import, the watermark advances to the newest processed message so re-running import does not re-scan or re-import earlier messages.
8. UI reads structured transactions and summaries from app services. Delete and export controls operate on structured local data and any generated local artifacts.

Raw SMS text is not stored after parsing except for one reviewed exception: the original SMS body of a matched, imported transaction is retained on that transaction, encrypted at rest, shown only on the local transaction detail screen, never logged, never transmitted, and removed by delete/purge. The raw source reference (for example a UPI reference number) and a masked account tail (for example the last four digits of a card) are retained on the transaction under the same rules. Sender identifiers, phone numbers, and separately-extracted account identifiers are still discarded after parsing. Only structured transaction fields, the retained original body, and the stored import watermark persist. Any raw SMS or transaction trail leaving the device (cloud, backup, AI, telemetry) remains disabled by default and requires a separate privacy review.

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
| Raw SMS is stored beyond the reviewed exception | High-sensitivity source data remains on device longer than needed | Retain only the original body of matched, imported transactions (encrypted, user-only, never logged or exported, purge-covered); keep all other raw-source diagnostics disabled by default and require privacy review for any wider retention. |
| Stored SMS body contains masked account tails, balances, or OTP-adjacent text | Sensitive substrings persist on device | Keep the body only inside encrypted persistence, exclude it from ToString/logs/telemetry/export, render it read-only with no copy or share, and cover it in purge tests. |
| Stored raw UPI reference is logged, exported to telemetry/AI, or otherwise leaks | User-identifiable payment reference exposure beyond the device | Keep the raw reference only inside encrypted transaction persistence, never log it or include it in telemetry or AI payloads, show it only in the local UI, and cover it in delete/purge tests. |
| Encrypted storage is bypassed or inconsistently used | Local transaction data may be readable from files or backups | Route real transaction persistence through encrypted storage, verify repository integration before release, and keep unencrypted test stores limited to synthetic data. |
| Keys or secrets are committed, logged, or stored with encrypted data | Encrypted content can be decrypted by unintended parties | Use platform key stores or developer secret stores, never commit keys, and exclude secrets from diagnostics. |
| Logs, telemetry, or crash reports include sensitive values | Sensitive data leaves the local trust boundary | Do not log raw SMS, sender identifiers, account hints, transaction references, merchant names from user data, UPI identifiers, phone numbers, or exact transaction trails. |
| SMS permission flow is unclear or too broad | Users may grant access without understanding local processing | Request permission only when needed, explain local processing in UI, support denial, and avoid background access beyond the Local MVP need. |
| Parser misclassification or duplicate failures corrupt summaries | Budgets and summaries become misleading | Keep parser deterministic, maintain synthetic coverage, preserve confidence scoring, and run focused Core tests after parser changes. |
| Sender address or raw SMS crosses the platform boundary into Core, logs, or storage | PII leaks beyond the minimal message-body and timestamp contract | Filter by sender on the Android side, drop the address before calling Core, pass only the message body and received timestamp, and keep raw SMS and identifiers out of logs and persistence. |
| High-confidence candidates auto-commit misclassified transactions | Budgets and summaries silently include wrong data | Auto-commit only High-confidence, non-duplicate candidates, route Medium and Low confidence to explicit bulk review, keep parsing deterministic with synthetic coverage, and provide an undo path for imported transactions. |
| Dedup keys miss or over-match during import | Duplicate transactions are stored, or genuine distinct transactions are dropped | Deduplicate with both the hashed reference and a composite key that includes minute-level timestamp, combine with the import watermark, and cover re-import idempotency and same-merchant same-day cases with tests. |
| Full-inbox scan mishandles large inboxes | UI stalls or memory pressure during the first import | Read the inbox in bounded pages off the UI thread, build dedup indexes once per batch instead of per-item full scans, and report scan progress. |
| Delete, export, or purge flows miss local artifacts | User cannot reliably remove or control sensitive data | Track persisted artifacts, implement delete/export before public release, and include encrypted store files, indexes, and backups in purge tests. |
| Future cloud, telemetry, or AI features reuse Local MVP assumptions | Data crosses network boundaries without consent and retention controls | Treat all networked features as future/review-required and document consent, retention, deletion, export, failure modes, and sanitized payload rules before implementation. |

## Current Gaps

- App-level transaction persistence is not complete.
- Android SMS permission consent is implemented in Settings; SMS import is approved under this reviewed design and is in progress. The inbox reader, batch import orchestration, dedup watermark, and import UI are not yet implemented.
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
