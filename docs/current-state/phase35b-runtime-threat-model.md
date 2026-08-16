# Phase 35B Runtime Threat Model

| Threat | Mitigation in Phase 35B | Residual risk |
|---|---|---|
| Forged identity/substitution | Exact profile, adapter, request, and policy matching; ambiguity fails closed | No signed provider registry yet |
| Unauthorized capability | Exact authorization scope and denied defaults | Authorization is in-memory |
| Request/result/artifact tampering | Canonical hashes, lineage checks, Phase 35A relationship validation | No durable receipt ledger |
| Readiness/lifecycle bypass | Explicit readiness gate and closed transition table | No external attestation |
| Malicious artifact | Opaque governed records, validation, quarantine, redaction metadata | No content scanner/corpus |
| Replay/timeout/retry abuse | Session identity, linked cancellation, finite timeout, Phase 35A retry classification, no loop | No persistent replay ledger or resource quotas |
| Audit tampering/disclosure | Immutable in-memory audit projection and identifier-only diagnostics | No protected persistence |
| Fake-provider activation | Production catalog has no executable adapter and no probing/config activation path | Future registration requires review and sandbox |

Phase 35C should address the residual trust, persistence, scanning, and conformance gaps before any executable provider is enabled.

