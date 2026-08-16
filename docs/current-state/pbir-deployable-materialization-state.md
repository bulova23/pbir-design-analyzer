# Deployable PBIR Materialization Current State

## Status And Roadmap Mapping

Repository Phase 30 implements original roadmap Phase 4B: **Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**.

Phase 29 remains the only modern PBIR serializer. Phase 30 accepts only a validated pbir-deployable-artifact/v1, its matching pbir-deployable-manifest/v1, an operation request, and a caller-supplied absolute local output base.

## Destination Contract

The caller supplies one safe target leaf below an existing absolute output base. Phase 30 writes the exact Phase 29 inventory below that leaf:

- definition.pbir
- definition/version.json
- definition/report.json
- definition/pages/pages.json
- page and visual definition files supplied by Phase 29

It never writes PBIR-Legacy root-level report.json, semantic-model files, PBIP project files, provider output, or deployment metadata.

Target names, artifact paths, canonical paths, and platform-specific path identities are validated before use. Traversal, rooted paths, separators in the target leaf, reserved control names, PBIP or SemanticModel targets, links, reparse points, special files, duplicate paths, platform case collisions, and paths outside the authorized target fail closed.

## Preview And Existing Destinations

Preview is filesystem-read-only and classifies a destination as:

- absent or empty: ready to create;
- exact artifact match: no changes;
- valid receipt-backed prior Phase 30 output: ready to replace as managed;
- arbitrary differing nonempty content: blocked conflict;
- tampered receipt, journal, managed target, or incomplete transaction: recovery required.

There is no force-overwrite option. A user-managed nonempty directory is never claimed or replaced.

## Validation And Determinism

Every preview and apply reruns Phase 29 artifact/manifest postflight validation. Before publication, every artifact document is also evaluated against the eight pinned Microsoft PBIR schemas embedded from reviewed commit 34356d97e1218c79331780f8f5b77b03f2d13f35. Schema references resolve only from the embedded set; no network resolution is available.

The existing JsonSchema.Net test suite remains the independent complete Draft 7 conformance oracle. The runtime evaluator covers every schema keyword present in the pinned fixture set used by the emitted Phase 29 subset.

Identical Phase 29 bytes remain byte-identical on disk. Paths, target inventories, lineage additions, diagnostics, journals, receipts, results, and hashes use deterministic ordering and caller-supplied identifiers; no timestamp or random identifier is invented.

## Transaction, Rollback, Cleanup, And Retry Behavior

Apply initializes an owned private control root below the output base, takes a target-scoped exclusive lock, rechecks the previewed target hash, writes all files to a same-filesystem staging directory with create-new semantics, verifies the complete staged inventory, moves any managed prior target to backup, and promotes the staging directory as a unit.

Canonical journals and receipts are flushed to disk and replaced through same-directory temporary files. A successful result is returned only after the promoted target, receipt, and completed journal are verified.

Rollback operates only on the current receipt transaction or a current interrupted transaction. Applied bytes move to quarantine rather than being deleted. The hash-verified backup and previous receipt are restored when present. Ambiguous or mutated state remains blocked or recovery required.

Transaction history, backups, staging remnants from failed work, and quarantine are retained intentionally. Phase 30 performs no automatic history cleanup. A terminal aborted transaction ID cannot be reused; callers retry with a new safe transaction ID after obtaining a fresh preview. This preserves evidence and prevents a retry from silently inheriting partial state.

Cross-platform process termination cannot make a multi-directory sequence perfectly crash-atomic. Persistent hash-chained phases make interruption detectable and permit recovery only from provable states.

Phase 31 adds cancellation checks at Phase 30 transaction-safe boundaries: before I/O, after exclusive-lock acquisition, during staging, after staging verification, and before the first target move. Cancellation before target mutation aborts the journal and propagates. Once promotion begins, the existing Phase 30 finish-or-restore behavior remains authoritative.

## Phase 31 Application Consumption

Repository Phase 31 exposes Phase 30 through PbirMaterializationOrchestrationService. It re-runs canonical Phase 29 serialization and read-only Phase 30 preview for every apply, compares the complete caller-carried preview identity, and delegates only a current create or managed-replacement preview to Phase 30 apply.

The Phase 31 result distinguishes absent, empty, exact match, managed replacement, conflict, recovery required, applied, stale preview, invalid request, unsafe destination, unsupported operation, schema failure, transaction reuse, cancellation, and failure. Recovery inspection uses only Phase 30 preview state and cannot invoke rollback.

Phase 31 returns fixed diagnostics and logical fields rather than Phase 30 absolute paths or raw filesystem exception messages. It does not change any journal, receipt, backup, quarantine, lineage, hash, schema, or target-layout contract.

## Trust Boundary And Remaining Exclusions

The preview-only local writer remains unchanged and is not a Phase 30 dependency. Phase 30 adds no provider invocation, Microsoft Skills execution, API or CLI invocation, deployment, publishing, Power BI Desktop automation, Analyzer automation, UI integration, refinement loop, Fabric App generation, or Fabric Data App generation.

Phase 31 is authorized separately and does not widen Phase 30 authority. No later repository phase is authorized by Phase 31.
