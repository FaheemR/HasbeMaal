---
description: "Use when adding Azure backup, sync, telemetry, AI, App Configuration, Key Vault, Functions, Container Apps, or deployment infrastructure."
applyTo: ["infra/**", ".azure/**", "src/Infrastructure/**/*.cs", ".github/workflows/**/*.yml"]
---

# Azure Guidelines

- Keep cloud features optional. The MVP is local-first.
- Use encrypted backups before queryable cloud sync.
- Use managed identity and Key Vault for server-side secrets.
- Do not send raw SMS, account identifiers, or personal transaction trails to Azure AI services.
- Application Insights telemetry must be PII-free and opt-in.
- Create a deployment plan before adding Azure infrastructure.