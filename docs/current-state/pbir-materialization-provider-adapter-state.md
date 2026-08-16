# PBIR Materialization Provider Adapter Current State

## Status

Repository Phase 33 implements the local PBIR materialization RPC adapter. Repository Phase 34 consumes that adapter from the existing VS Code Design Studio workflow. Repository Phase 32 remains the generic transport prerequisite.

Repository Phase 31 remains the application authority. Phase 33 exposes it through exactly three local routes: pbir/materialization/preview, pbir/materialization/apply, and pbir/materialization/recovery/inspect.

## Boundary and compatibility

The requested adapter cannot be treated as an existing-roadmap implementation detail for three reasons:

- Provider Adapter Framework, Execution Provider Contract Framework, and Runtime Provider Framework are planning or pre-execution contract seams. Their current-state documents explicitly exclude provider invocation and runtime-provider implementations.
- The original Phase 4 scope describes a Microsoft PBIR adapter that maps Generation Request intent, checks PBIP/PBIR prerequisites, and participates in structural validation. That is broader and semantically different from a transport wrapper over local Phase 31 materialization.
- RpcHost is the only shipped backend transport, but it has no per-request cancellation registry or cancellation notification handling. It processes requests serially and uses permissive JSON deserialization. Therefore cancellation, concurrent request handling, strict unknown-field rejection, bounded payload validation, interrupted responses, and provider-disconnect behavior are not existing lifecycle guarantees that a narrow adapter can merely reuse.

The approved roadmap keeps these seams separate: Phase 32 owns the shared transport lifecycle, Phase 33 owns only the local wire adapter, and the broader Microsoft PBIR runtime-provider remains a later provisional phase. Existing LanguageClient routes and initialize capabilities are unchanged; the new route registration adds no initialize capability.

The adapter is stateless and returns only safe identifiers, hashes, relative file metadata, lineage, typed outcomes, and fixed redacted diagnostics. It requires exact validated preview identity and a fresh transaction ID for apply. Preview and recovery inspection remain read-only. Phase 30 remains the only filesystem, lock, staging, journal, receipt, backup, rollback, and recovery authority.

## Transport Prerequisite

Repository Phase 32 provides the generic strict JSON, finite size, cancellation, concurrency, serialized response, disconnect, shutdown, cleanup, and redacted-diagnostic guarantees required before any application adapter is added.

Phase 33 reuses that prerequisite without adding a transport stack. Operation payload limits are lower than the Phase 32 request/payload/response limits. Strict field, version, identifier, destination, artifact-profile, and policy validation occurs before orchestration.

## Preserved State

- No provider, runtime-provider, Microsoft Skills, API, CLI, deployment, publishing, Desktop, Analyzer, or generated-artifact authority is added by Phase 34. Its one command and existing Design Studio materialize card are presentation/orchestration consumers only.
- Phase 33 does not change Phase 29–31 production contracts or services; it uses their existing internal orchestration boundary.
- No commit, push, pull request, merge, discard, or working-tree cleanup was performed.
