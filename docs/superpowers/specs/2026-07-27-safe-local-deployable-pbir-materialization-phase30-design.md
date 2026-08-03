# Safe Local Deployable PBIR Materialization — Repository Phase 30 Design

Status: Approved and implemented on 2026-08-02 for Repository Phase 30 / original roadmap Phase 4B only.

## Roadmap Mapping

Repository Phase 30 maps exactly to original roadmap Phase 4B:

**Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**

Repository Phase 29 implemented original roadmap Phase 4A serialization. It produces a validated, deterministic, in-memory modern PBIR artifact and manifest. Phase 30 may materialize only that file inventory to a local report-definition directory.

Phase 30 does not authorize the provider-execution portion of original Phase 4 or any later roadmap phase.

## Objective

Add a separate backend boundary that:

1. previews a proposed local materialization without changing the filesystem;
2. applies an explicitly approved, unchanged preview through a staged directory transaction;
3. rolls back the current successful apply, or recovers its interrupted transaction, from preserved local journal state;
4. verifies every written and restored byte against deterministic hashes; and
5. never reuses or widens the preview-only writer.

## Explicit Non-Goals

Phase 30 does not add:

- raw Design Package, Design Studio, or pbir-ir/v1 consumption;
- deployable serialization or artifact refinement;
- preview-writer reuse or new preview-writer authority;
- root-level PBIR-Legacy report.json;
- semantic-model files;
- .pbip files or complete PBIP project materialization;
- provider or Microsoft Skills execution;
- API, network, or CLI invocation;
- deployment or publishing;
- Power BI Desktop automation;
- Analyzer Workspace launch or validation;
- generated-artifact refinement loops;
- Fabric App or Fabric Data App generation;
- Design Studio controls or other UI changes;
- automatic transaction-history cleanup.

The target contains only the exact modern PBIR report-definition files supplied by Phase 29. Control journals, receipts, backups, staging directories, and rollback quarantine remain outside that target.

## Inputs And Trust Boundary

The materializer accepts only:

- PbirDeployableArtifact using pbir-deployable-artifact/v1;
- PbirDeployableManifest using pbir-deployable-manifest/v1;
- one operation-specific Phase 30 request;
- an explicit local output base directory.

It does not accept canonical IR, serializer requests, raw design objects, preview artifacts, preview manifests, generation manifests, providers, or deployment descriptors.

Every operation reruns PbirDeployableSerializerValidator.ValidateOutput over the artifact and manifest. The materializer does not trust a caller’s prior validation claim.

## Chosen Architecture

### Alternatives Considered

1. **Create-only writer**
   - Strength: smallest mutation surface.
   - Weakness: cannot safely update a directory previously written by Phase 30.

2. **Per-file journal and replacement**
   - Strength: can write into arbitrary existing directories.
   - Weakness: a failure can expose a mixed old/new PBIR hierarchy.

3. **Dedicated-directory staged swap — selected**
   - Strength: the full file set is staged and verified before the target changes; arbitrary user-managed directories remain protected.
   - Cost: the target must be a dedicated leaf directory on the same filesystem as its control area.

### Component Boundaries

- PbirDeployableMaterializationCanonicalJson
  - canonical operation records, inventory snapshots, fingerprints, and SHA-256 hashes;
  - no filesystem access.
- PbirDeployableMaterializationPathPolicy
  - validates the output base, target leaf name, resolved paths, and control paths;
  - rejects traversal, rooted target names, reserved control names, PBIP and semantic-model targets, and reparse points.
- IPbirDeployableMaterializationFileSystem
  - the only Phase 30 filesystem dependency;
  - exposes the bounded operations needed for inventory, exclusive locks, exact-byte writes, flush, directory moves, and journal persistence.
- PbirDeployableMaterializationFileSystem
  - production System.IO adapter;
  - no network, process, provider, or extension-host dependency.
- PbirDeployableMaterializationSafetyGate
  - validates contracts, Phase 29 artifact/manifest integrity, operation authority flags, path policy, preview/apply references, and transaction state.
- PbirDeployableMaterializationPreviewService
  - read-only target classification and materialization plan.
- PbirDeployableMaterializationTransactionStore
  - canonical journal, receipt, and exclusive-lock handling under the control root.
- PbirDeployableMaterializationApplyService
  - stages, verifies, promotes, and records a complete materialization transaction.
- PbirDeployableMaterializationRollbackService
  - restores the immediately preceding target state or recovers the current interrupted transaction.

The preview-only PbirLocalPreviewFileWriterService, its safety gate, content factory, models, and write manifest are not dependencies of any Phase 30 component.

## Local Directory Model

The caller supplies:

- outputBaseDirectory: an absolute existing local directory;
- targetDirectoryName: one normalized leaf name.

The target resolves to:

```text
[outputBaseDirectory]/[targetDirectoryName]/
```

The materializer writes Phase 29 relative paths below that target:

```text
definition.pbir
definition/version.json
definition/report.json
definition/pages/pages.json
definition/pages/[pageIdentity]/page.json
definition/pages/[pageIdentity]/visuals/[visualIdentity]/visual.json
```

It never writes root-level report.json.

The private materialization control root is:

```text
[outputBaseDirectory]/.pbir-design-analyzer/materialization/
```

targetKey is the first 32 lowercase hexadecimal characters of:

```text
normalizedTargetPath = NFC(canonicalTargetPath)
platformKeyPath = UPPER_INVARIANT(normalizedTargetPath) on Windows/macOS; normalizedTargetPath on Linux
targetKey = LOWER_HEX(SHA-256(UTF8(platformKeyPath)))[0..32]
```

The target key prevents target names from becoming control-path components.

The materialization control root contains:

```text
control-root.json
targets/[targetKey]/
```

control-root.json uses pbir-deployable-materialization-control-root/v1 and contains:

- schemaVersion;
- owner fixed to pbir-design-analyzer;
- purpose fixed to deployablePbirMaterialization;
- canonicalOutputBaseHash;
- controlRootHash.

If `.pbir-design-analyzer` or its materialization child already exists without the exact valid marker, preview blocks and apply does not claim or modify it. When the control root is absent, preview remains read-only and reports that apply must initialize it. Apply creates the marker with create-new semantics; a concurrent creator must produce the same validated marker or the operation fails closed.

A target transaction uses:

```text
targets/[targetKey]/transactions/[transactionId]/journal.json
targets/[targetKey]/transactions/[transactionId]/staging/
targets/[targetKey]/transactions/[transactionId]/backup/
targets/[targetKey]/transactions/[transactionId]/quarantine/
targets/[targetKey]/transactions/[transactionId]/previous-receipt.json
targets/[targetKey]/current-receipt.json
targets/[targetKey]/materialization.lock
```

transactionId is a request field matching:

```text
^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$
```

It is never generated from a timestamp or random value.

## Path And Filesystem Rules

The path policy requires:

- outputBaseDirectory is absolute, exists, is a directory, and is not a reparse point;
- targetDirectoryName is NFC, is one leaf segment, is not "." or "..", contains no directory separator, colon, control character, or trailing dot/space, and is not `.pbir-design-analyzer`;
- targetDirectoryName does not end in `.pbip` or `.SemanticModel`, case-insensitively;
- target, control, staging, backup, quarantine, and every ancestor inspected below the base are not symbolic links or reparse points;
- every artifact relative path passes the Phase 29 safe relative-path contract and resolves below the target;
- existing target entries are regular files or directories only;
- path comparison uses OrdinalIgnoreCase on Windows and macOS and Ordinal on Linux;
- artifact paths are unique under the active platform comparison;
- definition.pbir and the definition hierarchy remain mutually exclusive with root-level report.json.

Phase 30 does not follow links and does not write through links.

## Target Classification

Preview classifies the target as exactly one state:

- absent;
- emptyDirectory;
- exactArtifactMatch;
- managedPriorApply;
- conflict;
- recoveryRequired.

Rules:

- absent and emptyDirectory may be created;
- exactArtifactMatch produces a no-change preview and no apply transaction;
- managedPriorApply may be replaced only when current-receipt.json is valid and its target fingerprint matches current disk state;
- an unreceipted nonempty target that differs from the requested artifact is conflict;
- an incomplete current journal or mismatched managed receipt is recoveryRequired;
- conflict and recoveryRequired cannot be applied.

No explicit flag can convert an arbitrary conflict into an overwrite.

## Canonical Existing-Target Fingerprint

The target inventory covers every regular file below the target. Directories are derived and are not separate entries.

For an existing target, entries are ordered by normalized relativePath using the active platform path comparison and then serialized as UTF-8 JSON with:

- no byte-order mark;
- no indentation;
- no trailing newline;
- JSON property order shown below;
- lowercase hexadecimal SHA-256 values.

```json
{"schemaVersion":"pbir-deployable-target-inventory/v1","targetState":"files","files":[{"relativePath":"definition.pbir","byteLength":123,"hashSha256":"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}]}
```

The targetState value is:

- absent for a missing target;
- emptyDirectory for an existing empty target;
- files for a nonempty regular-file inventory.

Absent and empty inventories use an empty files array. targetStateHash is SHA-256 over the exact canonical UTF-8 bytes.

Duplicate normalized paths, case collisions on Windows, links, special files, unreadable files, hash failures, or inventory changes during the scan reject preview.

## Versioned Contracts

### pbir-deployable-materialization-preview-request/v1

Contains:

- schemaVersion;
- requestId;
- artifactRef and artifactHash;
- manifestRef and manifestHash;
- targetDirectoryName;
- executionPolicy;
- requestedOperation fixed to preview.

executionPolicy contains false values for:

- providerInvocationAllowed;
- microsoftSkillsExecutionAllowed;
- apiInvocationAllowed;
- cliInvocationAllowed;
- deploymentAllowed;
- publishingAllowed;
- desktopAutomationAllowed;
- analyzerAutomationAllowed.

Preview carries no filesystemMutationAllowed flag because preview is structurally read-only.

### pbir-deployable-materialization-control-root/v1

The exact ownership marker described in the local directory model. Its self-hash covers every marker field except controlRootHash. It grants only ownership of the private materialization control directory and carries no target, provider, deployment, or external execution authority.

### pbir-deployable-materialization-preview/v1

Contains:

- schemaVersion;
- previewId;
- requestRef;
- artifactRef, artifactHash, manifestRef, and manifestHash;
- canonical output base and target paths;
- targetKey;
- target classification;
- target inventory and targetStateHash;
- ordered planned files copied from the manifest;
- disposition: create, replaceManaged, noChanges, blockedConflict, or recoveryRequired;
- activeTransactionRef when recovery is required;
- rollbackAvailable for the current managed target;
- lineage;
- warnings and diagnostics;
- hashes: inputHash, plannedFileSetHash, targetStateHash, lineageHash, previewHash.

Preview is deterministic for identical artifact, manifest, request, canonical paths, and filesystem snapshot. It writes no directory, lock, receipt, journal, or file.

### pbir-deployable-materialization-apply-request/v1

Contains:

- schemaVersion;
- requestId and transactionId;
- previewRef and previewHash;
- artifactRef, artifactHash, manifestRef, and manifestHash;
- expectedTargetStateHash;
- applyApproved fixed to true;
- rollbackRequired fixed to true;
- executionPolicy with filesystemMutationAllowed true and every external authority false.

The request contains no alternate files, content, path root, or overwrite override.

### pbir-deployable-materialization-transaction/v1

Persisted journal fields:

- schemaVersion;
- transactionId;
- operation: apply or rollback;
- targetKey and canonical target path;
- previewRef and previewHash;
- artifact and manifest references and hashes;
- expected pre-state classification and hash;
- previousReceiptHash when present;
- phase;
- ordered journal events;
- stagingInventoryHash;
- backupInventoryHash when present;
- committedTargetStateHash when present;
- transactionHash.

Apply phases are:

- initialized;
- stagingWritten;
- stagingVerified;
- aborted;
- backupMoved;
- targetPromoted;
- targetVerified;
- receiptCommitted;
- completed;
- restoring;
- restored;
- recoveryRequired.

Each journal rewrite is canonical, flushed to disk, and hashed. Unknown phases fail closed.

### pbir-deployable-materialization-apply-result/v1

Contains:

- schemaVersion;
- resultId and requestRef;
- transactionId and transactionHash;
- previewRef and previewHash;
- disposition fixed to applied;
- canonical target path;
- written file inventory;
- previousTargetState and previousTargetStateHash;
- committedTargetStateHash;
- rollbackAvailable;
- currentReceiptHash when applied;
- lineage, warnings, diagnostics;
- hashes: inputHash, fileSetHash, lineageHash, resultHash.

Failed or interrupted apply returns no successful apply result. A caught failure returns diagnostics and recoveryRequired when automatic restoration cannot prove the pre-state.

### pbir-deployable-materialization-receipt/v1

current-receipt.json contains:

- schemaVersion;
- receiptId;
- transactionId;
- applyRequestRef and applyRequestHash;
- previewRef and previewHash;
- artifactRef, artifactHash, manifestRef, and manifestHash;
- targetKey and canonical target path;
- committedTargetStateHash;
- previousReceiptHash when present;
- rollbackTransactionRef;
- lineage;
- receiptHash.

The receipt grants only recognition of a directory previously written by Phase 30. It is not deployment or provider authority.

### pbir-deployable-materialization-rollback-request/v1

Contains:

- schemaVersion;
- requestId;
- transactionId;
- targetDirectoryName and targetKey;
- expectedTransactionHash;
- expectedCurrentReceiptHash when a committed apply exists;
- expectedCurrentTargetStateHash;
- rollbackApproved fixed to true;
- executionPolicy with filesystemMutationAllowed true and every external authority false.

Rollback may target only the current receipt transaction or the current interrupted transaction for the target. Historical noncurrent transactions cannot be rolled back out of order.

### pbir-deployable-materialization-rollback-result/v1

Contains:

- schemaVersion;
- resultId and requestRef;
- transactionId and transactionHash;
- restoredTargetState and restoredTargetStateHash;
- quarantinedAppliedStateHash when present;
- restoredReceiptHash when a previous receipt is reinstated;
- recoveryDisposition: rolledBackCommittedApply or recoveredInterruptedApply;
- lineage, warnings, diagnostics;
- hashes: inputHash, lineageHash, resultHash.

### Shared Diagnostics, Readiness, Lineage, And Hashes

All operation states use versioned:

- pbir-deployable-materialization-diagnostics/v1;
- pbir-deployable-materialization-readiness/v1;
- pbir-deployable-materialization-lineage/v1;
- pbir-deployable-materialization-hashes/v1.

Diagnostics are stable code/path/message records sorted by code, path, and message using ordinal comparison.

Readiness values are:

- incomplete;
- blocked;
- readyToCreate;
- readyToReplaceManaged;
- noChanges;
- applying;
- applied;
- recoveryRequired;
- rollingBack;
- rolledBack.

Only applied may contain an apply result. A noChanges preview is terminal and does not authorize or require apply. Only rolledBack may contain a rollback result.

Lineage preserves the complete immutable Phase 29 lineage and appends preview, apply, transaction, receipt, and rollback references without removing or mutating upstream entries.

## Preview Algorithm

1. Validate request schema, references, negative authority flags, and null safety.
2. Rerun full Phase 29 artifact/manifest postflight validation.
3. Validate output base, target leaf, platform path uniqueness, and artifact path containment.
4. Read the current target inventory without creating filesystem state.
5. Read and validate current receipt and current transaction journal if present.
6. Classify target state and choose one allowed disposition.
7. Construct ordered planned-file records from the Phase 29 manifest.
8. Compute canonical hashes and return the immutable preview.

Any failure returns no applicable preview plan and performs no filesystem mutation.

## Apply Algorithm

1. Revalidate artifact, manifest, apply request, preview hash, references, authority flags, and path policy.
2. Validate or create the exact control-root ownership marker, create the target-scoped control directory, and acquire materialization.lock with exclusive sharing.
3. Reread target, receipt, and journal state under the lock.
4. Require the current targetStateHash to equal expectedTargetStateHash and the preview snapshot.
5. Reject conflict, recoveryRequired, no-change, reused transactionId, or any active incomplete transaction.
6. Create a unique transaction directory and flush initialized journal.json.
7. Write every artifact file to staging with create-new semantics and exact UTF-8 bytes.
8. Flush each file, rescan staging, and require an exact match to the Phase 29 file inventory with no extra files.
9. If the target exists, move the entire target directory to backup.
10. Move staging to the target path on the same filesystem.
11. Rescan the target and require the exact Phase 29 inventory and targetStateHash.
12. Persist and flush current-receipt.json, preserving the prior receipt in previous-receipt.json.
13. Mark the journal completed and return the successful result.

If a caught failure occurs before target or receipt mutation, the service may mark the journal aborted only after a rescan proves the original target and receipt hashes are unchanged. If a caught failure occurs after the target moved, the service attempts restoration before returning. If restoration cannot be proven, it records recoveryRequired and never reports applied.

## Rollback And Recovery Algorithm

1. Validate request, authority flags, path policy, journal hash, current receipt when present, and exclusive target lock.
2. Require the request to identify the current transaction for the target.
3. Rescan current target and require expectedCurrentTargetStateHash.
4. Move the current applied target to quarantine; never delete it.
5. Restore backup to the target when the pre-state was emptyDirectory or managedPriorApply; leave the target absent when the pre-state was absent.
6. Restore previous-receipt.json as current-receipt.json, or remove the current receipt when no previous receipt existed.
7. Verify the restored target state and receipt hash.
8. Mark the journal restored and return rollback result.

For an interrupted apply, the same service uses the journal phase to restore the last provable pre-state. Ambiguous or hash-mismatched journal state remains recoveryRequired and is not guessed.

## Concurrency And Durability

- Apply and rollback acquire one exclusive target-scoped lock.
- Disk state is always revalidated after acquiring the lock.
- Staging, backup, target, and quarantine are all below the same output base so promotion uses same-filesystem directory moves.
- File and journal content is flushed before the next transaction phase.
- A successful result is returned only after target verification, receipt persistence, and completed journal persistence.
- Process termination can still interrupt a directory sequence. Persistent phases make the interruption detectable and recoverable; Phase 30 does not claim impossible cross-platform crash-atomicity.

## Determinism

Identical Phase 29 artifact and manifest, requests, canonical paths, and filesystem snapshots produce identical:

- previews and planned file ordering;
- target inventories and state hashes;
- staging bytes;
- lineage;
- diagnostics and warnings;
- receipts, journals, and result hashes.

Physical success depends on local filesystem state, but no identifier, timestamp, property, path, or content value is invented implicitly.

## Fail-Closed Conditions

No applicable preview, successful apply result, or successful rollback result is produced for:

- invalid or mismatched Phase 29 artifact/manifest;
- unsupported schema version;
- root-level report.json or nonmodern inventory;
- unsafe, rooted, escaping, reserved, PBIP, or semantic-model target;
- duplicate or platform-colliding paths;
- links, reparse points, special files, unreadable content, or changing inventory;
- arbitrary nonempty existing target;
- invalid, missing, stale, or tampered receipt or journal;
- preview, artifact, manifest, transaction, receipt, or target-state hash mismatch;
- target state changed after preview;
- unapproved apply or rollback;
- concurrent lock ownership;
- reused transaction identity;
- an existing private control directory without the exact Phase 30 ownership marker;
- partial or extra staging files;
- write, flush, move, verification, or restoration failure;
- any external execution authority flag.

## Test Strategy

Tests are deterministic and offline.

### Contract And Preview Tests

- exact versioned contract inventory;
- exact request/reference/hash mapping;
- exact private control-root marker and refusal to claim unrelated control directories;
- read-only preview proven by before/after filesystem snapshots;
- absent, empty, exact-match, managed, conflict, and recovery-required classification;
- path traversal, absolute target, reserved name, PBIP, semantic-model, link, case collision, and root report.json rejection;
- preview writer types are absent from Phase 30 constructors, fields, parameters, and return types.

### Apply Tests

- exact bytes and directory hierarchy for a new target;
- empty target replacement;
- managed prior-apply replacement;
- no arbitrary existing-directory replacement;
- no-change preview without mutation or apply authority;
- stale preview and target race rejection;
- exclusive-lock concurrency rejection;
- file-set, content, byte-length, and hash verification;
- injected failures before staging, after staging, after backup move, after promotion, and before receipt commit;
- successful automatic restoration or explicit recoveryRequired;
- no partial successful result.

### Rollback And Recovery Tests

- restore absent, empty, and managed prior states;
- restore the previous receipt chain;
- quarantine rather than delete the applied target;
- reject rollback after target mutation;
- reject noncurrent and reused transactions;
- recover each supported interrupted journal phase;
- preserve recoveryRequired for ambiguous state.

### Trust-Boundary Tests

Reflection and dependency tests prove:

- Phase 30 consumes only Phase 29 artifact/manifest plus Phase 30 requests and local path/time values;
- only IPbirDeployableMaterializationFileSystem can perform I/O for materialization services;
- no preview-writer, provider, Skills, HTTP, CLI, process, deployment, Desktop, Analyzer, extension-host, or Design Studio dependency exists;
- all external authority flags remain false and legal contract fields;
- no new production package is required.

Existing PbirLocalPreviewFileWriterService tests must remain byte- and authority-stable.

### Required Validation

- focused Phase 29 and Phase 30 backend tests;
- full backend xUnit suite with actual counts;
- all Jest suites with actual counts;
- TypeScript compilation;
- whitespace, placeholder, contradiction, contract-name, roadmap-mapping, scope, and changed-file checks.

## Documentation And Memory

On implementation completion, update:

- docs/ROADMAP.md;
- docs/current-state/architecture-gap-analysis.md;
- docs/current-state/pbir-modern-serializer-state.md;
- a new docs/current-state/pbir-deployable-materialization-state.md;
- the original seven-phase roadmap plan;
- .agent-memory/repo-map.md;
- .agent-memory/current-focus.md;
- .agent-memory/session-summaries.md;
- the Phase 30 session note.

Documentation must say:

- Repository Phase 30 implements only original roadmap Phase 4B;
- Phase 29 remains the only deployable serializer;
- Phase 30 writes only local modern PBIR report-definition files;
- preview writer authority is unchanged;
- provider execution and all other non-goals remain unimplemented;
- the next phase is not authorized by Phase 30.

## Implementation Outcome

The explicit Phase 30 implementation goal approved both this boundary and the implementation plan. The delivered implementation preserves the dedicated-directory staged-swap architecture, validates Phase 29 outputs against the embedded pinned Microsoft schema set at runtime with network access disabled, and leaves the preview-only writer unchanged.

Provider execution, Microsoft Skills execution, deployment, Desktop automation, Analyzer automation, legacy root-level report.json generation, UI integration, and later roadmap phases remain unauthorized and unimplemented.

## Separately Authorized Phase 31 Consumer

Repository Phase 31 now consumes Phase 30 through a narrow application orchestrator. It recreates and validates Phase 30 preview state before apply, passes a fresh transaction ID, exposes read-only recovery inspection, and maps failures to redacted typed results. Phase 30 remains the only filesystem, locking, staging, journal, receipt, backup, quarantine, rollback, recovery, schema, lineage, and hash authority.
