# Pull Request

## Summary

- TBD

## Privacy Checklist

- [ ] I did not add real SMS messages, sender IDs, account numbers, card numbers, UPI IDs, phone numbers, transaction references, exported databases, backups, screenshots with financial details, secrets, tokens, signing keys, or connection strings.
- [ ] Any examples, tests, logs, screenshots, or fixtures use synthetic redacted placeholders only.
- [ ] This change does not log raw SMS, exact transaction trails, account identifiers, merchant names from user data, or secrets.
- [ ] Raw SMS storage remains disabled by default, or any diagnostic path is explicit, encrypted, and purgeable.
- [ ] Any cloud, AI, telemetry, backup, sync, export, or retention behavior has been called out for privacy review.

## Validation

- [ ] `dotnet test tests\Core.Tests\Core.Tests.csproj`
- [ ] `dotnet build src\Mobile\Mobile.csproj -f net10.0-android`
- [ ] Other:

## Rollout Risks

- TBD
