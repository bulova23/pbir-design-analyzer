# PBIR Materialization Application Orchestration Current State

## Status And Roadmap Mapping

Repository Phase 31 is the first separately authorized application-integration slice of original roadmap Phase 4 after Repository Phase 29 / original Phase 4A serialization and Repository Phase 30 / original Phase 4B materialization.

It establishes the baseline application intake path for safe local modern PBIR report definitions. It is not external provider execution, Skills execution, deployment, publishing, PBIP generation, Desktop verification, Analyzer automation, or UI integration.

## Application Boundary

PbirMaterializationOrchestrationService is the sole Phase 31 composition boundary. It depends only on:

- PbirDeployableSerializerService
- PbirDeployableMaterializationPreviewService
- PbirDeployableMaterializationApplyService

It has no filesystem, path-policy, transaction-store, schema-evaluator, preview-writer, HTTP, process, extension-host, Skills, Desktop, Analyzer, deployment, or publishing dependency.

## Preview And Recovery Inspection

Preview runs canonical Phase 29 serialization, then canonical Phase 30 read-only preview. Results distinguish absent, empty, exact match, managed replacement, conflict, recovery required, invalid request, unsafe destination, unsupported operation, schema failure, cancellation, and failure.

Recovery inspection uses the same read-only path and may report one active transaction reference. It never invokes rollback or mutates transaction state.

## Explicit Apply

Apply requires:

- the complete validated preview identity;
- the original deterministic serialization/materialization inputs;
- explicit apply approval;
- the apply operation discriminator; and
- a new safe transaction ID.

The orchestrator reserializes and re-previews using the original preview request ID. Any artifact, manifest, preview, target-state, or hash mismatch returns stale preview. Only a current create or managed-replacement disposition reaches Phase 30 apply.

Phase 30 remains authoritative for exclusive locking, staged same-filesystem promotion, journals, receipts, backups, rollback quarantine, recovery state, immutable lineage, deterministic bytes, paths, and hashes.

## Cancellation, Concurrency, And Failure

Pre-cancelled requests return cancelled without work. Phase 30 apply checks cancellation before I/O, after lock acquisition, during staging, after staging verification, and before target mutation. Staging cancellation aborts the journal and does not expose a partial target. After target mutation begins, Phase 30 completes or restores its transaction.

Concurrent applies cannot both publish one preview. Phase 30 locking and target-state revalidation allow at most one commit; other callers fail closed through a typed stale, recovery, transaction-reuse, or failure result.

Transaction IDs are target-scoped, immutable, and non-reusable. A rejected or aborted attempt requires a fresh preview and new transaction ID.

## Diagnostics And Schema Gate

Application results never include raw Phase 29/30 messages or absolute output, control, transaction, staging, backup, or quarantine paths. Stable codes, logical fields, and fixed messages are returned instead.

Phase 30 continues to validate every candidate offline against the same eight embedded Microsoft PBIR schemas pinned to commit 34356d97e1218c79331780f8f5b77b03f2d13f35. Phase 31 adds no schema source, resolver, version, or network access.

## Remaining Exclusions

Skills execution, external provider invocation, deployment, publishing, Power BI Desktop automation, Analyzer automation, VS Code commands/dialogs/notifications/webviews, PBIP or semantic-model generation, PBIR-Legacy root-level report.json, schema upgrades, refinement loops, Fabric App generation, and Fabric Data App generation remain unimplemented.
