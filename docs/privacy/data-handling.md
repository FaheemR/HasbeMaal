# Data Handling

HasbeMaal is local-first and privacy-first. The repository must never contain real personal financial data.

See the [Local MVP threat model](threat-model.md) for assets, trust boundaries, current data flows, mitigations, and review triggers.
See the [Android SMS store review notes](android-sms-store-review.md) for `READ_SMS` permission purpose, consent copy, denial/revocation behavior, and store-review data constraints.

## Data Classes

- Public: source code, docs, synthetic fixtures, parser rules without personal examples.
- Sensitive: transaction amounts, merchant history, SMS text, sender IDs, account identifiers, UPI IDs, phone numbers, transaction references, exported databases, backups, screenshots.
- Secret: signing keys, API keys, connection strings, tokens, encryption keys.

## Storage Principles

- Encrypt local persistence before storing real transactions.
- Store encryption keys in Android Keystore and iOS Keychain where supported.
- Prefer extracted structured fields over raw SMS text. The one reviewed exception is the original SMS body of an imported matched transaction, retained encrypted for on-device display only (see below).
- Hash sender IDs when they are needed for duplicate detection; do not store sender IDs raw.
- Store the source transaction reference (for example a UPI reference number) both as a duplicate-detection hash and as a raw value. Retain the raw reference only inside encrypted transaction persistence, show it only to the user, never log it, and remove it in delete/purge.
- Provide export and delete controls before public release.

## Current Implementation Status

- `FileEncryptedStore` exists as an Infrastructure encrypted-file primitive.
- App-level transaction persistence is not fully wired to the mobile experience yet.
- Android SMS permission consent is implemented in Settings; SMS ingestion is not implemented yet. See the [Android SMS store review notes](android-sms-store-review.md).
- On-device SMS import retains, on each matched imported transaction, the original SMS body plus the raw UPI reference and a masked account tail (for example the last four digits of a card): encrypted at rest, shown only to the user on the detail page, never logged, and removed by delete/purge. Sender IDs, phone numbers, and separately-extracted account identifiers are still discarded after parsing.
- Raw SMS storage is limited to the original body of matched imported transactions, encrypted and user-only; no other raw source identifiers are stored.

## Telemetry Principles

- Telemetry is opt-in.
- Telemetry must not include raw SMS, sender IDs, account identifiers, UPI IDs, phone numbers, transaction references, merchant names, or exact transaction amounts.
- Use coarse counters such as parser success/failure counts and feature usage.

## AI Principles

- Use deterministic local logic for parsing and budgeting first.
- Use AI only for optional insights over sanitized summaries.
- Do not send raw SMS, sender IDs, transaction references, or personal transaction trails to AI services.
