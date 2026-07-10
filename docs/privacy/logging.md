# Privacy-Safe Logging

HasbeMaal logs are local diagnostics for development and debugging. They are not telemetry, analytics, crash upload, or cloud logging. Any remote logging or telemetry feature needs a separate privacy review before implementation.

## Allowed Fields

Logs may include:

- Component names and operation names.
- Coarse status values such as started, succeeded, skipped, failed, confirmed, or cancelled.
- Exception type names and stack traces produced without exception messages or file path details.
- Item counts.
- Year and month values.
- Static page route names without query strings or route parameters.
- Boolean outcomes.
- App-defined validation category names.

## Forbidden Fields

Logs must never include:

- Raw SMS text.
- SMS sender IDs.
- Phone numbers.
- Account numbers.
- UPI IDs.
- Transaction references.
- Source reference hashes.
- Merchant names from user data.
- Exact transaction amounts.
- Exported database paths or local storage paths.
- Encryption keys.
- Secure storage values.
- Raw serialized payloads.
- Full transaction trails.

Do not pass exception objects directly to `ILogger`. Exception messages can contain user-derived values, storage paths, or platform details. Log sanitized exception type and stack trace fields instead.

## Safe Examples

```text
Component=App Operation=CreateWindow Status=Started
Component=AppShell Operation=SetContentTemplate Status=Succeeded Route=Dashboard
Component=TransactionsViewModel Operation=Load Status=Succeeded Count=3
Component=EncryptedStore Operation=Load Status=Failed ExceptionType=System.InvalidDataException
Component=SmsTransactionParser Operation=TryParse Status=Succeeded Parsed=True Confidence=High
Component=SettingsPage Operation=PurgeLocalData Status=Cancelled
```

## Unsafe Examples

```text
RawMessage=Rs. 1250 paid to REDACTED STORE via UPI ref SYNTH001
Merchant=REDACTED STORE Amount=1250.00 Account=XX0000
StoragePath=/data/user/0/io.github.faheemr.hasbemaal/files/local-data/...
SourceReferenceHash=...
SecureStorageValue=...
Payload={...}
```

## Review Checklist

- The log is needed for local diagnosis and has a clear operation name.
- The log uses fixed message templates and structured fields.
- The log excludes raw SMS, identifiers, merchant names, exact amounts, storage paths, keys, secure storage values, payloads, and transaction trails.
- Exception logging does not pass the exception object to `ILogger` and does not include exception messages.
- Route logging strips query strings and route parameters.
- DEBUG logging providers remain local-only.
- Any telemetry, analytics, crash upload, cloud logging, AI analysis, backup, or sync behavior has a separate privacy review.