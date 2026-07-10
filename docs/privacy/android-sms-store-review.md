# Android SMS Store Review Notes

Last reviewed: 2026-07-10

These notes document the Android `READ_SMS` permission and privacy copy for store review. They apply to the Local MVP boundary described in [Data Handling](data-handling.md) and the [Local MVP threat model](threat-model.md).

## Review Summary

HasbeMaal is Android-first and local-first. The Android SMS permission is intended only for user-approved, on-device reading of existing SMS messages so the app can detect transaction candidates locally. SMS access is optional and must not be required for non-SMS workflows.

Current implementation status:

- Implemented: Android declares `android.permission.READ_SMS` in `src/Mobile/Platforms/Android/AndroidManifest.xml`.
- Implemented: Settings shows an explicit SMS permission consent section.
- Implemented: The permission flow supports allow, denied, unsupported, and open-app-settings states.
- Approved and in progress: user-initiated, foreground SMS inbox import that filters by a sender allowlist, parses locally, and stores only structured transactions. See [Approved SMS Import Flow](#approved-sms-import-flow).
- Not implemented and not approved: background SMS monitoring, raw-source diagnostics, cloud sync, telemetry, AI insights, or any network submission of SMS data.

## Permission Purpose

`READ_SMS` is requested so a user can choose to let HasbeMaal read historical SMS content on the device for local transaction detection. The intended data minimization rule is:

1. Read only the SMS content needed for local parsing.
2. Convert matching content into structured transaction candidates.
3. Store only structured transaction data through approved local encrypted persistence.
4. Do not retain raw SMS text, sender identifiers, source transaction references, account hints, UPI identifiers, or phone numbers after parsing.

SMS import is approved under the narrow purpose above and the controls in [Approved SMS Import Flow](#approved-sms-import-flow). The permission remains optional, and non-SMS workflows must continue to function when it is denied. Any change that widens this purpose, adds background access, or retains raw source data requires a new privacy review and a threat model update before implementation continues.

## Approved SMS Import Flow

SMS import runs only when the user opens the import flow in the foreground and starts it. The approved behavior is:

- Scan scope: the full inbox is read on the first import; later imports read only messages newer than a stored watermark timestamp, so earlier messages are not re-scanned.
- Sender allowlist: only messages from allowlisted senders are considered. The allowlist is seeded with common bank and UPI sender IDs and can be extended or reduced by the user. Sender addresses are used only to apply the allowlist and are dropped before any content is handed to local parsing. Sender addresses, phone numbers, and account hints are never stored or logged.
- Local parsing: only the message body and its received timestamp are passed to deterministic on-device parsing. The received timestamp becomes the transaction date. A source reference, when present, is reduced to a one-way hash for duplicate detection and is never stored raw.
- Deduplication: candidates are deduplicated against existing transactions and within the batch using the hashed reference and a composite key of amount, direction, received timestamp, and normalized merchant, combined with the watermark. Duplicates are skipped.
- Review model: high-confidence, non-duplicate candidates are committed as structured transactions in a single user action. Lower-confidence candidates are shown for grouped, bulk review before they are committed or discarded; un-actioned candidates stay pending for a later import, subject to a cap. Imported transactions can be removed through the app's delete controls.
- No raw retention: after import, only structured transaction fields and the watermark persist. No raw SMS text, sender identifier, phone number, account hint, UPI identifier, or raw reference is retained.

Final user-facing copy for the import screens will be documented here when the import UI lands.

## Consent Copy

The current Settings page uses this user-facing copy:

- Section title: `Historical SMS access`
- Body: `Optional Android permission for reading existing SMS messages locally on this device. HasbeMaal does not store or transmit raw SMS. You can deny access and revoke it later from app settings.`
- Primary action: `Allow SMS access`
- Secondary action: `Open app settings`
- Granted status: `SMS access is allowed.`
- Denied status: `SMS access is not allowed.`
- Unsupported status: `SMS access is only available on Android.`

Store-review copy should stay consistent with this statement:

> HasbeMaal uses Android SMS access only when you choose to enable it, to read messages on your device for local transaction detection. Raw SMS is processed locally, is not stored, and is not sent to HasbeMaal, cloud services, AI services, or telemetry. You can deny or revoke SMS access from Android app settings.

Do not add examples that contain real SMS text, UPI IDs, account numbers, phone numbers, transaction references, merchant trails, screenshots with financial data, secrets, or real transaction history.

## Denial And Revocation

Users can deny SMS access. In the current Settings flow, denial leaves the app in the `SMS access is not allowed.` state and keeps app settings available for later review.

Users can revoke SMS access through Android app settings. The app exposes an `Open app settings` action from the Settings page and rechecks permission state after that command returns.

SMS permission denial or revocation must not trigger data upload, telemetry submission, or raw SMS retention. Future SMS ingestion must also handle revoked permission by stopping SMS reads and continuing only with already-approved structured local data.

## Retention And Raw SMS Storage

Raw SMS storage is not enabled and must remain disabled by default.

Required handling rules:

- Do not persist raw SMS text.
- Do not persist sender IDs, account identifiers, UPI IDs, phone numbers, or source transaction references as raw values.
- Do not log raw SMS content or exact transaction trails.
- Do not include raw SMS or personal financial data in screenshots, issues, review attachments, parser fixtures, or generated artifacts.
- Any future raw-source diagnostic mode must receive a separate privacy review and must be explicit, encrypted, time-limited, and purgeable.

## Local-Only Processing

The Local MVP does not include network, cloud, AI, or telemetry processing for SMS data. SMS-derived parsing must run on device through deterministic local logic before any structured transaction data is stored.

Cloud sync, cloud backup, telemetry, crash reporting, and AI-generated insights are future review-required features. They must not receive raw SMS, sender identifiers, source transaction references, UPI identifiers, phone numbers, account hints, merchant trails, or exact transaction history.

## Screenshots And Review Data

Store-review screenshots and attachments must use safe states only:

- Use the Settings SMS consent card, empty states, or synthetic redacted data.
- Do not show real SMS inbox content.
- Do not show real transaction lists, budgets, account hints, phone numbers, UPI identifiers, sender IDs, or merchant trails.
- Do not attach exported databases, backups, logs, or diagnostic files from a real device.
- If a reviewer asks for sample data, provide synthetic redacted fixtures only.

## Current Gaps

- SMS import is approved under this review and is in progress; the inbox reader, batch import orchestration, dedup watermark, and import UI are not yet implemented.
- End-to-end encrypted transaction persistence is not fully wired to the mobile experience.
- Delete, export, and purge UX are not complete.
- Network, cloud, telemetry, and AI flows are not approved for SMS data.

Update this document before adding SMS ingestion, parser failure capture, raw-source diagnostics, telemetry, cloud sync, backup, AI insights, or any store-review screenshots that include app data.
