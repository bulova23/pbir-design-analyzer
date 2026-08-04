# Local PBIR RPC Adapter Current State

## Status

Repository Phase 33 is implemented as the first local transport integration slice after Phase 31 orchestration and Phase 32 RPC transport hardening.

Roadmap mapping:

- Phase 29 = original Phase 4A serialization.
- Phase 30 = original Phase 4B safe local materialization.
- Phase 31 = post-4B application orchestration.
- Phase 32 = RPC transport hardening.
- Phase 33 = local PBIR RPC adapter.
- Phase 34 onward remains provisional and unauthorized until separately approved.

## Routes and authority

The adapter registers exactly:

- pbir/materialization/preview
- pbir/materialization/apply
- pbir/materialization/recovery/inspect

It is provider-neutral, stateless, and internal to the local RpcHost. Preview and recovery inspection are read-only. Apply requires the exact Phase 31 validated preview identity and a fresh transaction ID. Every operation invokes PbirMaterializationOrchestrationService; no Phase 30 writer, filesystem, lock, journal, receipt, rollback, or recovery service is directly reachable through RpcHost.

No initialize capability was added, so valid existing LanguageClient traffic keeps its prior capability contract. Unknown operations remain Method Not Found.

## Contract and safety behavior

Each route has a versioned request contract and all routes return pbir-local-materialization-response/v1. Unknown, duplicate, missing, malformed, oversized, or unexpected fields are rejected. IDs, operation/version alignment, local destinations, links/reparse points, reserved targets, artifact profile, schema locks, dataset paths, semantic-binding collisions, and execution authority flags are checked before orchestration. Adapter request and response payload limits are 512 KiB and 2 MiB respectively, both below the Phase 32 transport limits.

Responses map all fifteen Phase 31 outcomes to stable safe strings. They exclude absolute paths, staging/journal/backup details, exception details, payload contents, and transaction internals. Only relative file inventory, hashes, immutable lineage, safe references, and bounded fixed diagnostics are returned.

## Lifecycle

The adapter delegates cancellation, duplicate active request arbitration, concurrency, frame writing, disconnect, and shutdown to Phase 32. Cancellation before dispatch prevents orchestration; in-flight cancellation reaches Phase 31; completion owns the response if it wins the terminal claim; repeated cancellation is idempotent. Concurrent applies remain governed by Phase 30 locking and transaction artifacts. Recovery inspection performs preview-only classification and has no rollback route.

## Explicitly out of scope

Provider invocation, Microsoft Skills execution, external APIs/CLI/network, UI, VS Code commands/views, Desktop, Analyzer handoff, deployment/publishing, PBIP or semantic-model generation, generated-artifact intake, authentication/authorization/encryption, remote transport, and legacy root-level report.json remain unimplemented.
