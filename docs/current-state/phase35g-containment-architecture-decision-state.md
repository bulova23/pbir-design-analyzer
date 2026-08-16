# Phase 35G Containment Architecture Decision — Current State

## Executive recommendation

The selected future architecture is **`remote-controlled-execution/v1`**. The decision is recorded but not enabled. `LocalMacOSProcess` remains **NotAdmitted**, exactly as Phase 35F established. No provider, fixture, PBIR generation, Desktop automation, secret, or remote worker was executed.

## Decision matrix

Weights reflect mandatory security/platform compatibility first; operational convenience cannot compensate for a provider-platform blocker. Scores are 1–5 evidence-based design ratings, not enforcement proof.

| Criterion | Weight | Virtualization.framework local Linux guest | Controlled remote Windows/Linux service |
| --- | ---: | ---: | ---: |
| Security isolation | 15 | 4 | 4 |
| Required provider platform | 20 | 1 — no Windows guest | 5 — Windows and Linux profiles |
| Filesystem/network/credential isolation | 12 | 4 | 4 |
| Child process/resource/timeout control | 10 | 3 | 4 |
| Identity, certification, replay, audit | 10 | 4 | 4 |
| Artifact scanning and validation | 8 | 4 | 4 |
| macOS compatibility | 6 | 5 on Apple Silicon | 4 — network/service dependency |
| Deployment/supportability | 7 | 2 | 3 |
| CI and developer reproducibility | 4 | 2 | 3 |
| Scalability | 3 | 2 | 5 |
| Long-term maintainability | 5 | 2 | 3 |
| Operational cost | 0 | — | — |
| **Weighted total** | **100** | **2.86 / 5** | **4.05 / 5** |

Virtualization is disqualified for the likely full provider requirement because Windows support is mandatory for Desktop-dependent execution. The total is not the reason for the decision; the mandatory platform capability is.

## Selected security model

```text
Local governance: authorization → exact certification → replay claim → policy/credential reference
       ↓ authenticated domain request
Remote authority: revalidate all above → allocate worker → enforce limits → audit
       ↓ quarantine manifest and hashes
Local intake: verify request/session/certification/lineage → scan → validate → accept or reject
```

Credentials are future short-lived opaque grants scoped to request, session, provider, and capability. They are never placed in the VM/worker image, environment, command line, or audit payload.

## Deployment and performance

Virtualization requires a signed native helper, entitlement, guest images, image updates, local disk, and Apple Silicon integration tests. Remote requires a private service, Windows worker image, optional Linux worker image, identity, queue, quarantine storage, patching, monitoring, and availability operations. Startup, transfer, parallelism, and resource overhead are **unknown** until Phase 35H measurements; no estimates are presented as facts.

## Failure and recovery

Every request has an idempotency key derived from request/session/provider/certification/policy identity. Lost acknowledgement triggers status reconciliation, not a second execution. Lost polling leaves the request pending and records an uncertainty event. Cancellation is sent once and raced against completion; completion wins only with a verified terminal remote record. Worker crash, service restart, transfer interruption, or local restart produces a recoverable state that must be reconciled before artifact acceptance.

## Phase 35H recommendation

Implement only the authenticated private domain-level protocol, one isolated Windows worker profile, independent server-side validation, replay/reconciliation state, correlated audit, and inert artifact-transfer proof. Do not add a real provider until those controls pass platform-specific tests.

