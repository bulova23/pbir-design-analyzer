# Phase 35B — Governed Runtime Provider Architecture & Execution Framework

## Decision

Phase 35B adds a narrow, backend-only composition root for coordinating a future provider. It consumes the authoritative Phase 35A contracts directly and adds runtime-only representations only for session state, validation events, diagnostics, artifact disposition, and audit projection. It does not add a real provider, RPC route, discovery probe, external execution, report generation, PBIR materialization, or artifact bytes.

The runtime is deliberately composed from focused services:

```text
Phase35A projected request
        |
        v
  Phase35B orchestrator
   |       |        |
 authorization  readiness  provider resolution
   |       |        |
   +-------+--------+
           v
      session factory
           |
      lifecycle coordinator
           |
      offline adapter
           |
 validation pipeline -> artifact intake -> audit projection
           |
     result / receipt / diagnostics
```

The composition root coordinates; gates validate; adapters describe and simulate only an offline result; intake decides disposition; audit records the decision path.

## Why This Shape

The monolithic service option would centralize policy, provider behavior, lifecycle, and output handling in a class that becomes difficult to test and change. A generic workflow engine would introduce a second abstraction language before there is a real provider and would obscure the strict Phase 35A boundaries. Focused services make dependency direction visible and keep each contract independently testable.

## Components

`Phase35BContracts.cs` contains runtime-only immutable records and closed enums. Phase 35A records remain authoritative for provider profile, request, authorization, policy, readiness, result, artifact, failure, retry, redaction, quarantine, hash, and lineage data.

`Phase35BProviderRegistry` holds an explicit immutable set of typed adapter registrations. `Phase35BProviderResolutionService` requires one exact provider identity, profile match, capability match, policy allowance, authorization scope, ready readiness, and one adapter. Zero and multiple matches fail closed.

`Phase35BAuthorizationGate` and `Phase35BReadinessGate` validate the exact Phase 35A records. The readiness gate accepts a supplied readiness snapshot so controlled tests can prove a ready path without changing the Phase 35A catalog, whose normal result remains `Unavailable`.

`Phase35BSessionFactory` creates immutable sessions from the validated request, profile, authorization, readiness snapshot, policy, and injected clock. State changes are replacement records created by `Phase35BLifecycleCoordinator`; mutable bags and implicit string states are not used.

`IPhase35BProviderAdapter` is a constrained offline adapter contract. It can validate a request, declare capabilities/readiness, describe a typed plan, and return a deterministic offline result. It has no command, process, path, URL, credentials, dynamic payload, delegate, or reflection escape hatch. The production catalog registers only the existing Phase 35A metadata and no adapter capable of generation.

`Phase35BValidationPipeline` runs typed stages in a fixed order and stops on the first failure. `Phase35BArtifactIntakeService` validates the Phase 35A result/artifact relationship and returns `Accepted`, `Rejected`, or `Quarantined`, preserving redaction and lineage metadata.

`Phase35BAuditProjectionService` produces an in-memory immutable audit record containing the request hash, provider, policy, decisions, lifecycle history, validation, artifact disposition, timing, and failure classification. `Phase35BDiagnostics` emits structured in-memory events with identifiers and outcomes only; raw payloads and secrets are excluded.

`Phase35BTimeoutCoordinator` uses linked cancellation tokens, an injected `TimeProvider`, and an explicit timeout policy. Caller cancellation and policy timeout map to different terminal states. No process termination or external cleanup is attempted.

## Execution Flow

1. The caller supplies the already-projected Phase 35A request and explicit Phase 35A governance records.
2. Request, profile, policy, authorization, and readiness are checked by focused gates.
3. Exact adapter resolution creates a session only after all gates pass.
4. The lifecycle coordinator records `Created`, `Validated`, `Authorized`, `Ready`, and `ProviderResolved`.
5. The offline adapter returns a typed fake result; it cannot produce a report artifact.
6. Result validation runs through fixed stages.
7. Fake artifact intake validates lineage, hashes, redaction, quarantine, and acceptance.
8. The audit projection captures every decision and the orchestrator returns a typed outcome.

Any gate failure returns a structured rejected/failed outcome without invoking an adapter. The normal Phase 35A catalog remains unavailable and therefore cannot reach the success path.

## Lifecycle

Runtime states are `Created`, `Validated`, `Authorized`, `Ready`, `ProviderResolved`, `Executing`, `ValidatingResult`, `ReviewingArtifact`, `Completed`, `Rejected`, `Failed`, `Cancelled`, `TimedOut`, and `Quarantined`. The coordinator owns a closed transition table; terminal states have no outgoing transitions. Phase 35A lifecycle remains unchanged and is represented in audit as the authoritative contract lifecycle when a result is produced.

## Validation and Artifact Disposition

The fixed stage sequence is request contract, policy, authorization, readiness, provider compatibility, result, artifact, and acceptance validation. A stage returns a named result with stable failure code and no side effects. Artifact intake rejects schema, identity, lineage, hash, or validation violations; quarantines unsafe or explicitly quarantined outputs; and accepts only a valid, release-eligible artifact. Redaction is metadata-only in this phase. No content is transformed.

## Retry

Phase 35B consumes `Phase35ARetryPolicy` and `Phase35AFailureClass` values. It reports whether a failure is contractually retryable but does not run an uncontrolled loop. No exception-name, status-code, provider-string, or heuristic classification is introduced.

## Threat Model

| Threat | Trust boundary | Mitigation | Residual / future requirement |
|---|---|---|---|
| Forged identity or provider substitution | Request to registry | Exact provider/profile/adapter identity and one-match resolution | Phase 35C signed provider registration |
| Unauthorized capability | Authorization/policy gate | Exact request, provider, capability, artifact, and policy hash scope; deny defaults | Durable authorization/audit storage |
| Request, result, hash, or lineage tampering | Contract records | Canonical SHA-256 hashes and relationship validation | Persistent tamper-evident receipt store |
| Readiness spoofing or lifecycle bypass | Gate/state coordinator | Explicit snapshot checks and closed transitions | Independent attestation/sandbox |
| Malicious artifact | Adapter to intake | Opaque artifact contract, validation, quarantine, redaction metadata, no release while unsafe | Output scanning corpus and sandbox |
| Replay | Request/session boundary | Request IDs, hashes, explicit session identity, no implicit retry loop | Durable replay ledger and nonce policy |
| Cancellation/timeout abuse or denial of service | Caller to adapter | Linked tokens, finite timeout, bounded policy, no blocking APIs | Resource quotas and isolated worker |
| Retry amplification | Failure to coordinator | Consume bounded Phase 35A retry policy; no automatic loop | Per-provider budget and circuit breaker |
| Audit tampering or diagnostic disclosure | Runtime to observer | Immutable in-memory audit projection and redacted structured events | Append-only protected audit persistence |
| Accidental fake-provider activation | Catalog/configuration | Production catalog has no executable adapter and no probing/config activation path | Explicit reviewed provider registration and sandbox |

## Performance and Maintainability

The runtime uses immutable records, bounded arrays, fixed stage lists, async adapter calls, and no blocking I/O. The composition root has no provider-specific branches. `PbirScoringService` remains untouched; if a later provider needs PBIR information, a narrow projection seam must be introduced rather than coupling runtime orchestration to scoring internals.

## Phase 35C Boundary

The next phase should not immediately activate a generation provider. The highest-risk prerequisites are a reviewed provider trust/sandbox boundary, credential isolation, durable tamper-evident audit, an artifact scanning corpus, conformance tests, and explicit output validation. Only after those are designed should a provider-specific adapter be considered.

