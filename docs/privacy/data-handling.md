# Data Handling

HasbeMaal is local-first and privacy-first. The repository must never contain real personal financial data.

See the [Local MVP threat model](threat-model.md) for assets, trust boundaries, current data flows, mitigations, and review triggers.

## Data Classes

- Public: source code, docs, synthetic fixtures, parser rules without personal examples.
- Sensitive: transaction amounts, merchant history, SMS text, sender IDs, account identifiers, UPI IDs, phone numbers, transaction references, exported databases, backups, screenshots.
- Secret: signing keys, API keys, connection strings, tokens, encryption keys.

## Storage Principles

- Encrypt local persistence before storing real transactions.
- Store encryption keys in Android Keystore and iOS Keychain where supported.
- Prefer extracted structured fields over raw SMS text.
- Hash sender IDs and source transaction references when they are needed for duplicate detection.
- Provide export and delete controls before public release.

## Current Implementation Status

- `FileEncryptedStore` exists as an Infrastructure encrypted-file primitive.
- App-level transaction persistence is not fully wired to the mobile experience yet.
- Android SMS ingestion and permission flows have not been implemented yet.
- Raw SMS storage is not enabled and must remain disabled by default.

## Telemetry Principles

- Telemetry is opt-in.
- Telemetry must not include raw SMS, sender IDs, account identifiers, UPI IDs, phone numbers, transaction references, merchant names, or exact transaction amounts.
- Use coarse counters such as parser success/failure counts and feature usage.

## AI Principles

- Use deterministic local logic for parsing and budgeting first.
- Use AI only for optional insights over sanitized summaries.
- Do not send raw SMS, sender IDs, transaction references, or personal transaction trails to AI services.
