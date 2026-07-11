# Research Spike — GPay Payee Mapping via Notification Access

> **Status: RESEARCH ONLY — NOT APPROVED FOR IMPLEMENTATION.**
> This document scopes an approach and its risks so it can be reviewed. No production code may be
> written until **Privacy Security**, **Product**, and **Architecture** all explicitly approve.
> Tracks Epic [#45](https://github.com/FaheemR/HasbeMaal/issues/45), Slice E.

Last updated: 2026-07-10

## 1. Problem

Bank and UPI SMS messages that HasbeMaal already imports (Slices A–D) carry the amount, date, and a
UPI reference, but **no payee/merchant name**. The deterministic parser therefore yields
`merchant = "Unknown"` for most UPI debits, which weakens reports, budgets, and forecasts.

Google Pay (and similar UPI apps) post an Android **notification** at payment time that typically
contains the human-readable counterparty, for example *"You paid ₹250 to Some Merchant"*. If that
payee could be associated with the corresponding imported transaction, the "Unknown" merchant could
be enriched into something useful — **on device, without changing the SMS pipeline**.

This spike evaluates whether that association is worth building, and at what privacy and policy cost.

## 2. Candidate approach

- Android `NotificationListenerService` (requires the `BIND_NOTIFICATION_LISTENER_SERVICE`
  permission and a user grant through **Settings → Notification access**, which cannot be requested
  with a normal runtime permission dialog).
- Filter to the **Google Pay package only** (`com.google.android.apps.nbu.paisa.user`). Ignore every
  other app's notifications.
- From a matching notification, extract only the fields needed to enrich a transaction: the payee
  name, the amount, and — if present — a reference token. Everything else is discarded immediately.
- **Join** the extracted payee to an already-imported transaction. Preferred join key is the UPI
  reference (now retained per Slice C); fallbacks are amount + a tight time window. On a confident
  match, set/replace the transaction's merchant with the payee name.

### Sketch (conceptual only)

```text
GPay notification ──(GPay package filter)──► platform capture
      │ payee, amount, ref?  (raw text dropped)
      ▼
on-device join (UPI ref, else amount+time window)
      │
      ▼
enrich existing FinancialTransaction.Merchant   (no new raw source stored by default)
```

Layering (consistent with the repo's boundaries): capture and package filtering live in the Android
platform layer; the join/enrichment contract lives in Core; persistence stays in Infrastructure. No
MAUI or platform types cross into Core.

## 3. Why this is high-sensitivity

Notification access is one of Android's most powerful capabilities: once granted, the service can
read **every** app's notifications, not just GPay. That makes this fundamentally different from the
existing SMS import, which is user-initiated, foreground, sender-allowlisted, and drops the address
before Core. A background listener is a persistent, broad surface. Treat it accordingly.

## 4. Threat model deltas (relative to `docs/privacy/threat-model.md`)

| Concern | Risk | Required mitigation (if ever built) |
| --- | --- | --- |
| Broad notification access | Service can read all apps' notifications, not only GPay | Hard package allow-list (GPay only) at the earliest point; ignore and never buffer anything else. |
| Persistent background capture | Passive, always-on collection unlike foreground SMS import | Opt-in, revocable, clearly disclosed; capture only while enabled; no background upload of any kind. |
| Payee name is new PII | Counterparty identity is more identifying than amount/date | Store only the enriched `Merchant`; do not store raw notification text by default; never log the payee. |
| Raw notification retention | High-sensitivity source data lingers | No raw-notification store by default; any diagnostic capture must be separately reviewed, encrypted, time-limited, and purgeable (same rule as raw SMS). |
| Mis-join enriches the wrong transaction | Wrong payee attached to a payment | Require a high-confidence key (UPI reference first); keep amount+time joins conservative; make enrichment reversible/editable. |
| Leakage via logs/telemetry/AI | Payee crosses the local trust boundary | Never log the payee; exclude from telemetry and any AI payload; keep it inside encrypted local persistence and the local UI only. |
| Purge/delete misses enriched data | User cannot remove enriched PII | Enriched merchant lives on the existing transaction record, already covered by delete/purge; add explicit purge tests. |

This must be added to the threat model's "Future, Review-Required" list before any implementation and
promoted into the main flows only after approval.

## 5. Consent and UX requirements

- **Opt-in only**, off by default, with a plain-language explanation of what is read (GPay payment
  notifications) and what is not (all other notifications are ignored).
- One clear entry point that deep-links to the system Notification-access screen; the app cannot
  grant this itself.
- Easy in-app disable and a pointer to revoke access in system settings.
- Explain the benefit ("fills in who you paid") and the limitation (GPay only, on device only).

## 6. Google Play policy considerations

- Use of notification access is **restricted** by Google Play. Apps must use the
  `BIND_NOTIFICATION_LISTENER_SERVICE` API only for a policy-permitted, user-facing core feature and
  must complete the required **declaration** with a prominent disclosure and rationale.
- There is a real **risk of rejection or removal** if reviewers judge the feature non-core or the
  disclosure insufficient. This is a launch/compliance risk, not just an engineering task.
- **Open item:** confirm the current Play Developer Program policy wording and any declaration form
  and demo-video requirements before committing — policy text changes and must be verified, not
  assumed, at implementation time.

## 7. Privacy rules that would be non-negotiable

- On-device only. No cloud, no sync, no AI use of the payee (consistent with existing AI/telemetry
  principles in `docs/privacy/data-handling.md`).
- GPay package only; everything else ignored at capture time.
- No raw notification storage by default; only the derived merchant persists, encrypted at rest.
- Never log the payee or raw notification text (extends `docs/privacy/logging.md`).
- Included in existing delete/purge; covered by tests.

## 8. Open questions

- Do current GPay notifications reliably include a UPI reference that matches the SMS reference? If
  not, the join degrades to amount + time, which is weaker and more error-prone.
- Notification text format is undocumented and can change across GPay versions and locales — how do
  we keep extraction deterministic and testable without real notification fixtures (synthetic,
  redacted only)?
- What is the correct behavior when GPay batches/updates a notification, or when multiple payments
  occur close together?
- Should enrichment be automatic on a confident match, or always user-confirmed?
- Battery/lifecycle cost of a bound listener; behavior across reboots and OEM background limits.
- Is there a lower-risk alternative that avoids notification access entirely (for example a manual
  "name this transaction" flow) that captures most of the value?

## 9. Required approvals before any code

1. **Privacy Security** — threat-model update, consent copy, retention, logging, and purge rules.
2. **Product** — is GPay payee enrichment a core, user-facing feature that justifies the Play risk,
   or is a manual naming flow sufficient?
3. **Architecture** — boundary design for capture (platform) → join contract (Core) → persistence
   (Infrastructure), and the join-key strategy.

## 10. Recommendation

Proceed **only** if Product deems the payee enrichment a core feature worth the notification-access
Play risk. If so, the least-risky first step is a **narrow, opt-in, GPay-only, UPI-reference-join**
design with no raw-notification retention, gated behind the three approvals above. Otherwise, prefer
the non-privileged alternative (manual/edit-merchant flow) already implied by the transaction detail
page from Slice D. **No production code until this document is reviewed and approved.**
