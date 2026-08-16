# PBIR Materialization Application Orchestration — Repository Phase 31 Design

Date: 2026-08-02

Status: Approved, implemented, and validated on 2026-08-02. Changes remain uncommitted.

## Roadmap Mapping

Repository Phase 31 is the first separately authorized application-integration slice of original roadmap Phase 4 after completed Phase 4A and Phase 4B.

- Original Phase 4A → Repository Phase 29: deterministic in-memory modern PBIR serialization.
- Original Phase 4B → Repository Phase 30: safe local deployable PBIR materialization.
- Repository Phase 31 → the bounded post-4B application orchestration seam that makes the completed local PBIR path consumable behind the certified provider architecture.

This mapping is supported by the original Phase 4 requirement to establish the baseline artifact intake path, its explicit reservation of separately authorized later execution work after Phase 4B, and the architecture-readiness rule that execution implementation starts behind certified provider contracts without moving orchestration ownership out of Planning Framework.

Phase 31 does not implement broader Microsoft Skills, external-provider, PBIP, Desktop-verification, deployment, or publishing work. Original Phases 5–7 remain unstarted as execution phases.

## Objective

Add one backend application service that composes, without reimplementing:

1. canonical Phase 29 serialization;
2. read-only Phase 30 preview and destination classification;
3. explicit Phase 30 apply using an unchanged validated preview identity and fresh transaction ID; and
4. read-only recovery inspection from Phase 30 preview state.

## Alternatives Considered

Direct provider access to Phase 30 services is rejected because callers could duplicate validation, outcome mapping, and redaction. A stateful in-memory preview-ticket registry is rejected because it would add restart, expiry, persistence, and concurrency semantics.

The chosen design is a stateless revalidation orchestrator. Apply repeats Phase 29 serialization and Phase 30 read-only preview from the original inputs, compares the caller-carried validated preview identity, and only then delegates to Phase 30 apply. This preserves deterministic bytes and makes stale state fail closed without an orchestration cache.

## Contracts And Outcomes

Phase 31 adds versioned application contracts for preview, apply, recovery inspection, validated preview identity, typed result, and redacted diagnostics.

The common outcome set is: absent, empty, exactMatch, managedReplacement, conflict, recoveryRequired, applied, stalePreview, invalidRequest, unsafeDestination, unsupportedOperation, schemaFailure, transactionReused, cancelled, and failure.

Preview identity contains the Phase 30 preview ID, preview hash, target-state hash, artifact reference/hash, manifest reference/hash, and original preview request ID. It contains no mutation authority. Apply carries the complete identity plus a separate apply request ID and a fresh safe transaction ID.

## Application Flow

Preview validates its operation and contract, checks cancellation, calls PbirDeployableSerializerService, and rejects any non-serialized result. It constructs the canonical Phase 30 preview request with preview-only authority, calls PbirDeployableMaterializationPreviewService, and maps Phase 30 target state and disposition to the typed Phase 31 result. No write-capable service is called.

Apply validates contract, approval, operation, preview identity, transaction ID, and cancellation. It reruns canonical Phase 29 serialization and Phase 30 preview using the original preview request ID. Any identity or target-state mismatch returns stalePreview. Conflict and recovery-required state return before apply. Only a current create or managed-replacement preview is projected into PbirDeployableMaterializationApplyRequest with local-mutation-only authority and delegated to PbirDeployableMaterializationApplyService.

Recovery inspection reuses the same serialization and read-only preview path. It reports recoveryRequired and the active transaction reference when Phase 30 can identify one safely. It never invokes rollback or mutates transaction state.

## Cancellation And Concurrency

Cancellation is checked before serialization, before and after preview, after exclusive-lock acquisition, during staging before each file write, and immediately before the first target mutation. Cancellation before mutation returns cancelled with no target change. Cancellation during staging is journaled and restored by Phase 30 before propagation. Once target promotion begins, Phase 30 finishes or restores rather than abandoning ambiguous state.

Phase 30 remains the exclusive-lock owner. Concurrent applies from one preview serialize safely: at most one commits; later contenders receive stalePreview, transactionReused, or recoveryRequired.

## Diagnostics And Dependency Boundary

Phase 31 never returns raw Phase 29/30 diagnostic messages or canonical filesystem paths. It maps codes to stable public codes, logical fields, and fixed messages. Unknown failures become a generic failure diagnostic. Artifact-relative PBIR inventory paths may remain visible, but absolute output, control, transaction, staging, backup, and quarantine paths are redacted.

Provider-facing or future host code may depend on the Phase 31 orchestrator and contracts only. The orchestrator may depend on PbirDeployableSerializerService, PbirDeployableMaterializationPreviewService, and PbirDeployableMaterializationApplyService. It may not depend on the materialization filesystem, path policy, transaction store, schema evaluator, preview-only writer, process, HTTP, Skills, Desktop, Analyzer, deployment, or publishing services.

## Preserved Guarantees

- Phase 29 remains the only serializer.
- Phase 30 remains the only materialization transaction implementation.
- Preview remains filesystem-read-only.
- Apply requires exact validated preview identity and a fresh transaction ID.
- Exclusive locks, staged promotion, journals, receipts, backups, rollback quarantine, immutable lineage, deterministic bytes, paths, and hashes remain owned by Phase 30.
- Runtime schema validation remains offline and pinned to the same eight Microsoft schemas.
- Unsafe destinations, stale state, invalid contracts, unsupported operations, schema failures, transaction reuse, cancellation, and filesystem failures fail closed.
- The preview-only writer remains unchanged and outside the dependency graph.

## Tests

Focused backend coverage proves contracts and every typed outcome; preview/apply/recovery behavior; fresh preview and transaction enforcement; cancellation and concurrency safety; eight-schema rejection; diagnostic redaction; application dependency boundaries; and unchanged Phase 29–30 behavior.

## Out Of Scope

Skills execution, external provider invocation, deployment, publishing, Power BI Desktop automation, Analyzer automation, VS Code UI integration, PBIP or semantic-model generation, PBIR-Legacy root-level report.json, schema upgrades, unrelated cleanup, and Git publication remain excluded.

## Implementation Outcome

The stateless application orchestrator, typed contracts, transaction-safe cancellation points, fixed diagnostic redaction, and contract/orchestration/cancellation/concurrency/stale-preview/recovery/boundary tests were delivered without duplicating Phase 29 or Phase 30 logic. Validation passed 14 Phase 31 tests, 111 focused Phase 29–31 tests, 665 full backend tests with zero failures or skips, 105 Jest suites / 527 Jest tests, standalone TypeScript compilation, and the eight-test offline schema/boundary gate over exactly eight pinned schema resources.
