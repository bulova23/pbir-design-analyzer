# Local PBIR RPC Adapter — Repository Phase 33 Design

Date: 2026-08-04

Status: Authorized for implementation in this session; changes remain uncommitted.

## Roadmap mapping and boundary

Repository Phase 33 is the first local transport integration slice after the completed Phase 31 application orchestration and Phase 32 RPC transport hardening:

- Repository Phase 29 = original Phase 4A deterministic modern PBIR serialization.
- Repository Phase 30 = original Phase 4B safe local PBIR materialization.
- Repository Phase 31 = post-4B application orchestration over those services.
- Repository Phase 32 = generic RPC transport hardening.
- Repository Phase 33 = provider-neutral local PBIR RPC adapter over Phase 31.
- Repository Phase 34 onward remains provisional and unauthorized until separately approved.

This phase adds no provider invocation, Microsoft Skills execution, UI, deployment, publishing, Desktop, Analyzer, PBIP, semantic-model generation, legacy root-level report.json, external transport, authentication, authorization, or new filesystem/writer authority.

## Architecture decision

The adapter is an internal, stateless request/response boundary in RpcHost. It receives the already framed and parsed JSON-RPC request from the existing SimpleJsonRpcServer, strictly validates one operation contract, constructs the existing Phase 31 request type, invokes only PbirMaterializationOrchestrationService, and maps the typed result to a safe versioned response. It does not cache previews, transactions, locks, artifacts, or recovery state; Phase 30 control artifacts and Phase 31 orchestration remain authoritative.

RpcHost will reference the existing Core project rather than source-linking a second copy of PBIR services. Core exposes its existing internal Phase 31 types only to the packaged RpcHost assembly through an explicit InternalsVisibleTo boundary. The dispatcher receives the adapter as a dependency and registers exactly three new routes:

- pbir/materialization/preview
- pbir/materialization/apply
- pbir/materialization/recovery/inspect

The initialize response advertises no new capability, preserving existing LanguageClient behavior. Route registration is documented and unknown routes retain the existing Method Not Found response.

## Wire contracts

Each operation has a distinct adapter contract version and an exact object shape. The JSON-RPC envelope remains owned by Phase 32; adapter payloads are bounded independently and use no more than the transport request/payload/response limits.

### Request envelopes

The adapter accepts only these fields at the operation payload root:

- schemaVersion: `pbir-local-materialization-rpc-request/v1`
- requestId: safe identifier, unique among active JSON-RPC requests
- operation: one of the three route operation names
- input: the complete Phase 31 materialization input

Preview input contains the existing canonical IR state, serializer request, deployable serializer request, output base directory, and target directory name. Apply additionally requires:

- validatedPreview: the exact Phase 31 preview identity, including all hashes and references
- transactionId: a fresh safe transaction identifier
- applyApproved: true

Recovery inspection additionally requires previewRequestId. It is read-only and invokes Phase 31 inspection only.

The adapter rejects unsupported versions, unknown operations, duplicate or unexpected fields at every object level, malformed envelopes, invalid IDs, unsafe paths, traversal, links, reserved targets, collisions, unsupported artifact profiles, malformed manifests, invalid transaction identifiers, stale preview identities, reused transaction IDs, and nonzero external-authority policy flags before orchestration. Preview and recovery inspection use read-only policies; apply permits only local mutation through Phase 31.

### Response envelope

The adapter emits `pbir-local-materialization-rpc-response/v1` with:

- requestId
- operation
- outcome
- validatedPreview when Phase 31 returns one
- transactionId only for a successful apply
- activeTransactionRef and rollbackAvailable when safe
- relative writtenFiles with byte lengths and SHA-256 hashes only
- lineage and target/result hashes when safe
- bounded diagnostics containing fixed codes, safe field names, and redacted messages

Absolute paths, staging paths, journal contents, backup paths, exception details, payload contents, and transaction internals never cross the adapter boundary. Diagnostics never include raw operation/request identifiers or local paths.

## Outcome mapping

Every PbirMaterializationOrchestrationOutcome is mapped explicitly and stably:

| Phase 31 outcome | Adapter outcome |
| --- | --- |
| Absent | absent-destination |
| Empty | empty-destination |
| ExactMatch | exact-match |
| ManagedReplacement | managed-replacement |
| Conflict | conflict |
| RecoveryRequired | recovery-required |
| Applied | applied |
| StalePreview | stale-preview |
| InvalidRequest | invalid-request |
| UnsafeDestination | unsafe-destination |
| UnsupportedOperation | unsupported-operation |
| SchemaFailure | schema-failure |
| TransactionReused | transaction-reused |
| Cancelled | cancelled |
| Failure | failure |

Handler faults fail closed as a fixed adapter failure response through the existing transport error path. Cancellation is decided by the Phase 32 request registry: cancellation before dispatch prevents orchestration; cancellation during execution is propagated to Phase 31; completion wins after the terminal response claim; repeated cancellation is idempotent; disconnect cancels and drains the request without creating a second response. Concurrent previews are independent. Concurrent applies are independently dispatched and Phase 30 locking/transaction truth determines contention, conflict, reuse, or failure.

## Compatibility and transport reuse

No existing LanguageClient request contract changes. Existing routes, JSON-RPC framing, limits, cancellation, concurrency, response serialization, shutdown, and disconnect lifecycle remain owned by SimpleJsonRpcServer. The adapter adds no transport implementation and no initialize capability. Adapter payload limits are explicit and lower than or equal to RpcTransportOptions. The operation payload is parsed from the existing bounded ParamsUtf8 memory before deserialization so oversized or structurally ambiguous input is rejected without unbounded allocations.

## Testing strategy

Use deterministic in-memory JSON-RPC streams and synchronization seams. Tests cover strict serialization, version/field/ID validation, every operation, every Phase 31 outcome, exact preview and fresh transaction enforcement, hostile destinations and artifact profiles, duplicate IDs, cancellation races, concurrent preview/apply behavior, disconnect cleanup, response correlation/frame integrity, diagnostic redaction, handler faults, no-mutation recovery inspection, and route/scope boundaries. Existing Phase 29–32 suites remain regression gates.

## Long-term risk review

1. Highest risk: duplicating Phase 31 or Phase 30 contracts in the adapter would drift hashes and compatibility. The adapter therefore reuses Core internal types and maps only the safe wire response.
2. High risk: exposing filesystem transaction details would make the local protocol a de facto writer API. The response DTO is deliberately redacted and relative-path-only.
3. High risk: treating cancellation or disconnect as rollback authority would corrupt transaction truth. Cancellation is propagated; Phase 30 remains the only rollback/recovery authority.
4. Medium risk: widening the route or version contract early would make future provider integration harder. Exactly three routes and one version are supported.

