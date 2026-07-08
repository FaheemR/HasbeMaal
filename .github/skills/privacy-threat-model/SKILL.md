---
name: privacy-threat-model
description: "Use when reviewing HasbeMaal privacy, security, threat modeling, local encryption, raw SMS handling, logs, telemetry, AI data sharing, backup, sync, export, delete, consent, and retention risks."
---

# Privacy Threat Model Workflow

## Procedure

1. Identify data entering the feature.
2. Classify it as public, sensitive, or secret.
3. Trace where it is stored, logged, transmitted, exported, backed up, or used for AI.
4. Check consent, retention, deletion, and recovery behavior.
5. Check test fixtures and docs for accidental personal data.
6. Propose concrete mitigations before implementation continues.

## Default Requirements

- Local-first storage.
- Encryption before real transaction storage.
- No PII in logs or telemetry.
- No raw SMS storage by default.
- Cloud and AI features are opt-in.