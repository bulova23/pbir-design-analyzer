# Safe Local Deployable PBIR Materialization — Repository Phase 30 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Materialize only validated Phase 29 modern PBIR artifacts into a safe local report-definition directory through deterministic preview, apply, rollback, and interrupted-transaction recovery controls.

**Architecture:** Add a new backend-only deployable materialization boundary. Preview is read-only; apply writes and verifies the complete inventory in a same-filesystem staging directory before a directory swap; rollback restores the current transaction’s preserved pre-state from a canonical journal and receipt chain. The preview-only writer remains unchanged and is not a dependency.

**Tech Stack:** .NET 8, System.IO behind a Phase 30-specific internal interface, System.Text.Json, SHA-256, xUnit, temporary-directory integration tests, and an injected fault filesystem for deterministic failure tests. No new production package.

---

Status: Proposed for approval. Do not execute this plan until the user explicitly approves both the Phase 30 boundary and this implementation plan.

## File Map

Create:

- `service-dotnet/Services/Discovery/Models/PbirDeployableMaterializationModels.cs`
  - versioned preview, apply, transaction, receipt, rollback, readiness, diagnostics, lineage, and hash contracts.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationCanonicalJson.cs`
  - exact inventory, journal, receipt, result serialization and SHA-256 helpers.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationPathPolicy.cs`
  - output-base, target-leaf, containment, platform-comparison, reserved-name, link, and collision rules.
- `service-dotnet/Services/Discovery/IPbirDeployableMaterializationFileSystem.cs`
  - bounded filesystem operations used by preview, apply, transaction, and rollback services.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationFileSystem.cs`
  - production System.IO implementation.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationSafetyGate.cs`
  - common contract, Phase 29 validation, authority, reference, path, snapshot, receipt, and journal checks.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationPreviewService.cs`
  - read-only snapshot, classification, plan, and preview hashing.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationTransactionStore.cs`
  - exclusive lock, canonical journal, current receipt, and transaction-directory persistence.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationApplyService.cs`
  - stage, verify, swap, restore-on-caught-failure, receipt, and apply result.
- `service-dotnet/Services/Discovery/PbirDeployableMaterializationRollbackService.cs`
  - current-transaction rollback and interrupted-apply recovery.
- `service-dotnet/tests/Discovery/PbirDeployableMaterializationContractTests.cs`
  - exact contract and canonical byte/hash tests.
- `service-dotnet/tests/Discovery/PbirDeployableMaterializationPreviewServiceTests.cs`
  - read-only preview and target classification.
- `service-dotnet/tests/Discovery/PbirDeployableMaterializationApplyServiceTests.cs`
  - physical materialization, concurrency, race, and fault restoration.
- `service-dotnet/tests/Discovery/PbirDeployableMaterializationRollbackServiceTests.cs`
  - rollback, receipt-chain restoration, quarantine, and recovery.
- `service-dotnet/tests/Discovery/PbirDeployableMaterializationBoundaryTests.cs`
  - precise callable/dependency authority tests and preview-writer regression.
- `docs/current-state/pbir-deployable-materialization-state.md`
  - delivered Phase 30 boundary and remaining execution gap.
- `.agent-memory/sessions/2026-07-27-pbir-deployable-materialization-phase30.md`
  - approval, implementation, validation, and stop-boundary record.

Modify:

- `docs/ROADMAP.md`
- `docs/current-state/architecture-gap-analysis.md`
- `docs/current-state/pbir-modern-serializer-state.md`
- `docs/current-state/pbir-local-preview-writer-state.md`
- `docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md`
- `.agent-memory/current-focus.md`
- `.agent-memory/repo-map.md`
- `.agent-memory/session-summaries.md`

Do not modify these production boundaries:

- `PbirLocalArtifactWriter*`
- `PbirLocalPreviewFileWriter*`
- preview artifact, preview manifest, preview package, or review-handoff contracts;
- VS Code extension or webview production code;
- provider, Microsoft Skills, deployment, Desktop, or Analyzer code.

## Task 1: Lock Contracts And Canonical Bytes

**Files:**

- Create: `service-dotnet/Services/Discovery/Models/PbirDeployableMaterializationModels.cs`
- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationCanonicalJson.cs`
- Create: `service-dotnet/tests/Discovery/PbirDeployableMaterializationContractTests.cs`

- [ ] **Step 1: Write failing contract inventory tests.**

Assert exact schema versions:

```text
pbir-deployable-materialization-preview-request/v1
pbir-deployable-materialization-preview/v1
pbir-deployable-materialization-control-root/v1
pbir-deployable-materialization-apply-request/v1
pbir-deployable-materialization-transaction/v1
pbir-deployable-materialization-apply-result/v1
pbir-deployable-materialization-receipt/v1
pbir-deployable-materialization-rollback-request/v1
pbir-deployable-materialization-rollback-result/v1
pbir-deployable-materialization-diagnostics/v1
pbir-deployable-materialization-readiness/v1
pbir-deployable-materialization-lineage/v1
pbir-deployable-materialization-hashes/v1
pbir-deployable-target-inventory/v1
```

Assert enums expose only the design-approved target states, dispositions, journal phases, recovery dispositions, and readiness values.

- [ ] **Step 2: Write failing canonical target-inventory tests.**

Use reversed input order and assert exact UTF-8 bytes:

```json
{"schemaVersion":"pbir-deployable-target-inventory/v1","targetState":"files","files":[{"relativePath":"definition.pbir","byteLength":123,"hashSha256":"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}]}
```

Assert:

- no BOM;
- no indentation or trailing newline;
- ordinal canonical property order;
- lowercase hexadecimal SHA-256;
- absent and emptyDirectory serialize with an empty files array;
- platform-colliding entries are rejected before canonicalization.

- [ ] **Step 3: Write failing self-hash coverage tests.**

Mutate one field at a time in the control-root marker, preview, journal, receipt, apply result, and rollback result. Include:

- artifact and manifest references/hashes;
- canonical paths and target key;
- target state and inventory;
- planned files;
- transaction phase and events;
- previous receipt hash;
- staging, backup, committed, restored, and quarantine hashes;
- lineage;
- warnings and diagnostics;
- every nested hash except the self-hash under evaluation.

Every mutation must change the owning contract hash or fail validation.

- [ ] **Step 4: Run the red gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableMaterializationContractTests
```

Expected: FAIL because Phase 30 contracts and canonical helpers do not exist.

- [ ] **Step 5: Implement the minimum records and canonical helper.**

Use positional internal records with JsonPropertyName attributes, immutable IReadOnlyList fields, explicit schema-version constants, and no implicit timestamps or identifiers.

Hash methods must accept complete payload fields and omit only the owning self-hash. Do not build hashes from partial reference projections.

- [ ] **Step 6: Run the contract tests.**

Expected: PASS.

- [ ] **Step 7: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): add deployable materialization contracts
```

## Task 2: Add The Bounded Filesystem And Path Policy

**Files:**

- Create: `service-dotnet/Services/Discovery/IPbirDeployableMaterializationFileSystem.cs`
- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationFileSystem.cs`
- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationPathPolicy.cs`
- Extend tests: `service-dotnet/tests/Discovery/PbirDeployableMaterializationContractTests.cs`

- [ ] **Step 1: Write failing path-policy tests.**

Cover:

- absolute existing output base accepted;
- relative or missing base rejected;
- base reparse point rejected;
- one NFC target leaf accepted;
- ".", "..", separators, rooted paths, colon, control characters, trailing dot/space, and `.pbir-design-analyzer` rejected;
- `.pbip` and `.SemanticModel` targets rejected case-insensitively;
- artifact traversal and root-level report.json rejected;
- Windows/macOS case folding and Linux ordinal behavior;
- target key is 32 lowercase hexadecimal characters derived from NFC canonical target path.

- [ ] **Step 2: Define the exact filesystem interface.**

The interface may expose only:

```text
GetFullPath
DirectoryExists
CreateDirectory
EnumerateEntries
GetEntryKind
OpenRead
OpenCreateNew
OpenExclusiveLock
MoveDirectory
MoveFile
DeleteFile
ReadAllBytes
WriteAllBytesCreateNew
WriteAllBytesReplace
FlushToDisk
```

If implementation proves one operation unnecessary, omit it. Do not expose general command execution, network access, unrestricted delete-directory, globbing, or recursive copy.

- [ ] **Step 3: Write failing adapter tests with temporary directories.**

Assert:

- create-new never overwrites;
- replacement is used only for canonical journal/receipt files;
- exclusive lock rejects concurrent ownership;
- entry classification detects regular file, directory, and reparse point;
- same-base directory moves preserve exact bytes;
- no method follows a rejected link.

- [ ] **Step 4: Run the red gate.**

Expected: FAIL because the interface, adapter, and policy do not exist.

- [ ] **Step 5: Implement the path policy and production adapter.**

Keep System.IO references inside the adapter and path policy. Use UTF-8 without BOM for canonical control JSON. Use FileStream.Flush(flushToDisk: true) for staged files and control records.

- [ ] **Step 6: Run the focused tests.**

Expected: PASS.

- [ ] **Step 7: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): add materialization filesystem boundary
```

## Task 3: Implement Read-Only Materialization Preview

**Files:**

- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationSafetyGate.cs`
- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationPreviewService.cs`
- Create: `service-dotnet/tests/Discovery/PbirDeployableMaterializationPreviewServiceTests.cs`

- [ ] **Step 1: Write a Phase 29 input fixture helper.**

Reuse the existing Phase 29 test fixture builder by extracting only a shared test helper if needed. Do not move production serializer logic into tests and do not make Phase 29 public.

- [ ] **Step 2: Write failing valid-preview tests.**

For absent and empty targets, assert:

- Phase 29 ValidateOutput passes again;
- disposition is create;
- planned file inventory equals the manifest exactly;
- physical paths resolve below the target;
- target inventory and hashes are canonical;
- preview lineage contains complete Phase 29 lineage;
- output base, target, control root, and transaction directories are unchanged before and after preview.

- [ ] **Step 3: Write failing target-classification tests.**

Cover:

- exact unreceipted artifact match → noChanges;
- valid current receipt plus matching disk fingerprint → replaceManaged;
- differing unreceipted nonempty target → blockedConflict;
- incomplete current journal → recoveryRequired;
- stale or tampered receipt → recoveryRequired;
- receipt target or artifact mismatch → recoveryRequired.

- [ ] **Step 4: Write failing trust and input rejection tests.**

Reject with no applicable preview:

- null nested request contracts;
- artifact/manifest mismatch or hash tampering;
- unsafe base, target, artifact path, link, special file, duplicate path, and platform collision;
- target inventory changing between enumeration and hashing;
- any external authority flag;
- unsupported operation or schema version.

- [ ] **Step 5: Run the red gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableMaterializationPreviewServiceTests
```

Expected: FAIL because the safety gate and preview service do not exist.

- [ ] **Step 6: Implement the minimum safety gate and preview service.**

The service must call PbirDeployableSerializerValidator.ValidateOutput directly. Inventory all regular files, reject unsafe entries, reread metadata needed to detect scan races, and construct an immutable preview only after classification succeeds.

Preview code must not call CreateDirectory, OpenCreateNew, WriteAllBytesCreateNew, WriteAllBytesReplace, MoveDirectory, MoveFile, or DeleteFile.

- [ ] **Step 7: Run the preview tests.**

Expected: PASS.

- [ ] **Step 8: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): add read-only deployable materialization preview
```

## Task 4: Add Canonical Transaction And Receipt Persistence

**Files:**

- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationTransactionStore.cs`
- Extend tests: `service-dotnet/tests/Discovery/PbirDeployableMaterializationContractTests.cs`

- [ ] **Step 1: Write failing transaction-store tests.**

Assert:

- one target-scoped exclusive lock;
- exact control-root ownership marker creation and validation;
- an unrelated or tampered existing `.pbir-design-analyzer` directory is never claimed;
- transactionId regex and create-new transaction directory;
- transaction reuse rejected;
- canonical journal phase progression only;
- unknown, skipped-backward, or terminal-phase rewrite rejected;
- every journal rewrite validates its previous transaction hash;
- current receipt replacement is flushed and hash-verified;
- previous receipt bytes are preserved exactly;
- tampered or truncated journal/receipt rejected;
- transaction paths remain below the target control root.

- [ ] **Step 2: Define allowed phase transitions.**

Encode an explicit transition table:

```text
initialized -> stagingWritten | aborted
stagingWritten -> stagingVerified | aborted
stagingVerified -> backupMoved | targetPromoted | aborted | restoring
backupMoved -> targetPromoted | restoring
targetPromoted -> targetVerified | restoring
targetVerified -> receiptCommitted | restoring
receiptCommitted -> completed | restoring
restoring -> restored | recoveryRequired
```

No transition leaves aborted, completed, or restored. aborted is allowed only when a rescan proves that target and receipt state never changed. recoveryRequired may transition only to restoring under an explicit rollback/recovery request.

- [ ] **Step 3: Run the red gate.**

Expected: FAIL because the transaction store does not exist.

- [ ] **Step 4: Implement the transaction store.**

Persist journal and receipts through write-new-temporary, flush, and same-directory file replacement. Validate canonical bytes and hashes on every read. Never infer a missing phase or receipt.

- [ ] **Step 5: Run the focused tests.**

Expected: PASS.

- [ ] **Step 6: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): add materialization transaction journal
```

## Task 5: Implement Apply With Staging And Restoration

**Files:**

- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationApplyService.cs`
- Create: `service-dotnet/tests/Discovery/PbirDeployableMaterializationApplyServiceTests.cs`

- [ ] **Step 1: Write failing new-target apply test.**

Given an approved create preview:

- stage exact Phase 29 bytes;
- verify all hashes and lengths;
- promote one complete target directory;
- persist a valid receipt and completed journal outside the target;
- return applied only after final target fingerprint equals the artifact;
- emit no file outside target and its private control root.

- [ ] **Step 2: Write failing empty and managed-replacement tests.**

Assert:

- empty target moves to backup before promotion;
- managed prior target moves to backup with all bytes preserved;
- previous receipt bytes are preserved;
- arbitrary nonempty target cannot be replaced;
- target mutation after preview rejects before staging;
- receipt mutation rejects before staging.

- [ ] **Step 3: Write failing concurrency and identity tests.**

Assert:

- concurrent lock holder blocks apply;
- transactionId reuse rejects;
- artifact, manifest, preview, or target-state hash mismatch rejects;
- applyApproved false, rollbackRequired false, or any external authority rejects;
- noChanges preview cannot be applied.

- [ ] **Step 4: Write failing injected-failure tests.**

Use a test filesystem wrapper that throws at:

- first staging file;
- middle staging file;
- staging verification;
- backup move;
- target promotion;
- target verification;
- receipt persistence;
- completed journal persistence.

For each stage assert:

- no successful result;
- no partially staged directory is exposed as target;
- pre-mutation failures end in aborted only after the unchanged target and receipt state is hash-proven;
- the pre-state is automatically restored and hash-proven when possible;
- otherwise readiness is recoveryRequired with a persisted journal;
- existing user-managed content is never deleted.

- [ ] **Step 5: Run the red gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableMaterializationApplyServiceTests
```

Expected: FAIL because apply does not exist.

- [ ] **Step 6: Implement the minimum apply service.**

Follow the approved algorithm in the design exactly. Do not add per-file target writes, overwrite overrides, cleanup, provider calls, UI callbacks, or deployment hooks.

- [ ] **Step 7: Run the apply tests.**

Expected: PASS.

- [ ] **Step 8: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): apply deployable materialization transaction
```

## Task 6: Implement Current-Transaction Rollback And Recovery

**Files:**

- Create: `service-dotnet/Services/Discovery/PbirDeployableMaterializationRollbackService.cs`
- Create: `service-dotnet/tests/Discovery/PbirDeployableMaterializationRollbackServiceTests.cs`

- [ ] **Step 1: Write failing committed-apply rollback tests.**

Cover pre-states:

- absent → current target moves to quarantine and target becomes absent;
- emptyDirectory → current target moves to quarantine and empty backup returns;
- managedPriorApply → current target moves to quarantine, previous backup and receipt return.

Verify all restored and quarantined inventory hashes.

- [ ] **Step 2: Write failing rollback safety tests.**

Reject:

- noncurrent historical transaction;
- stale transaction, receipt, or target hash;
- mutated applied target;
- missing backup for a pre-state that requires one;
- link or special file introduced after apply;
- rollbackApproved false;
- external authority;
- concurrent lock;
- repeated rollback of restored transaction.

- [ ] **Step 3: Write failing interrupted-phase recovery tests.**

For every nonterminal apply phase, construct exact on-disk journal state and assert the service either:

- proves and restores the pre-state; or
- preserves recoveryRequired without guessing.

Specifically test crashes:

- before target mutation;
- after backupMoved;
- after targetPromoted;
- after targetVerified;
- after receiptCommitted.

- [ ] **Step 4: Run the red gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableMaterializationRollbackServiceTests
```

Expected: FAIL because rollback/recovery does not exist.

- [ ] **Step 5: Implement rollback and recovery.**

Quarantine current applied bytes before restoration. Restore only from hash-verified backup and previous receipt bytes. Never recursively delete target, backup, quarantine, or transaction history.

- [ ] **Step 6: Run rollback tests.**

Expected: PASS.

- [ ] **Step 7: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): rollback deployable materialization transaction
```

## Task 7: Lock Trust Boundaries And Preview Compatibility

**Files:**

- Create: `service-dotnet/tests/Discovery/PbirDeployableMaterializationBoundaryTests.cs`
- Extend tests: `service-dotnet/tests/Discovery/PbirLocalPreviewFileWriterServiceTests.cs`

- [ ] **Step 1: Write precise callable-surface tests.**

Assert preview accepts only artifact, manifest, preview request, and output base.

Assert apply accepts only artifact, manifest, validated preview, apply request, and output base.

Assert rollback accepts only a rollback request containing the target leaf/key and the output base; it consumes persisted transaction state and does not accept invented replacement content.

- [ ] **Step 2: Write precise dependency tests.**

Allowed service dependencies:

```text
PbirDeployableSerializerValidator
PbirDeployableMaterializationCanonicalJson
PbirDeployableMaterializationPathPolicy
PbirDeployableMaterializationSafetyGate
PbirDeployableMaterializationTransactionStore
IPbirDeployableMaterializationFileSystem
```

Assert Phase 30 constructors, fields, parameters, and return types contain no:

- PbirLocalPreviewFileWriterService;
- PbirLocalPreviewFileWriterSafetyGate;
- PbirLocalPreviewFileContentFactory;
- PbirLocalWriteManifest;
- provider/runtime interfaces;
- HttpClient;
- Process;
- CLI, deployment, Desktop, Analyzer, Design Studio, or extension-host services.

Do not use broad forbidden-token source scans. Negative authority contract fields remain legal.

- [ ] **Step 3: Add preview-writer regression tests.**

Run the existing ready preview write fixture before and after Phase 30 service construction and assert:

- byte-identical preview files;
- identical result hashes;
- deployable artifacts remain rejected;
- its dependency and callable surface has not changed.

- [ ] **Step 4: Assert no production package expansion.**

PbirDesignAnalyzer.Core.csproj must add no filesystem abstraction package, schema package, network package, CLI package, provider project, Desktop project, or extension-host reference.

- [ ] **Step 5: Run the combined boundary gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirDeployableMaterializationBoundaryTests|FullyQualifiedName~PbirLocalPreviewFileWriterServiceTests|FullyQualifiedName~PbirDeployableSerializer"
```

Expected: PASS.

- [ ] **Step 6: Commit only when authorized.**

Proposed commit:

```text
test(pbir): lock materialization trust boundary
```

## Task 8: Update Roadmap, Current State, And Memory

**Files:**

- Create: `docs/current-state/pbir-deployable-materialization-state.md`
- Create: `.agent-memory/sessions/2026-07-27-pbir-deployable-materialization-phase30.md`
- Modify the documentation and memory files listed in the file map.

- [ ] **Step 1: Update original roadmap mapping.**

State exactly:

- Original Phase 4A → Repository Phase 29, deterministic in-memory modern PBIR serialization;
- Original Phase 4B → Repository Phase 30, safe local deployable PBIR materialization;
- provider and Microsoft Skills execution portions of original Phase 4 remain not started;
- original Phases 5–7 remain not started as execution phases.

- [ ] **Step 2: Document the delivered writer boundary.**

Record:

- accepted Phase 29 contracts;
- target and control directory structure;
- preview classifications;
- staged apply and receipt/journal behavior;
- rollback and recovery behavior;
- exact authority exclusions;
- preview writer independence;
- test counts and any known filesystem durability limitation.

- [ ] **Step 3: Name the next boundary without authorizing it.**

State that no provider-execution, Skills, Desktop, Analyzer, deployment, or publishing phase is authorized. Do not invent a repository phase number for the next work.

- [ ] **Step 4: Close repository memory.**

Preserve the unrelated Rayfin research record. Record Phase 30 approval, implementation, tests, changed boundaries, and stop condition compactly.

- [ ] **Step 5: Run document checks.**

Check:

- no placeholders or superseded contract names;
- one active session heading;
- no claim that Phase 30 serializes artifacts;
- no claim that preview writer gained deployable authority;
- no provider/execution authorization;
- Phase 29 and Phase 30 roadmap mapping consistency.

- [ ] **Step 6: Commit only when authorized.**

Proposed commit:

```text
docs: record Phase 30 materialization boundary
```

## Task 9: Final Verification And Stop

- [ ] **Step 1: Run the full focused backend gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirDeployableMaterialization|FullyQualifiedName~PbirDeployableSerializer|FullyQualifiedName~PbirLocalPreviewFileWriterServiceTests"
```

Record actual counts.

- [ ] **Step 2: Run the full backend suite.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

Record passed, failed, and skipped counts.

- [ ] **Step 3: Run all Jest suites.**

Run:

```bash
cd vscode-extension && npm test
```

Record suite and test counts.

- [ ] **Step 4: Run TypeScript compilation.**

Run:

```bash
cd vscode-extension && npm run compile
```

Record the result.

- [ ] **Step 5: Inspect final scope and status.**

Run:

```bash
git diff --check
git status --short
```

Confirm:

- Phase 29 and unrelated edits were preserved;
- only approved Phase 30 backend, test, documentation, and memory files changed;
- no writer output or test transaction directory is left in the repository;
- no root-level report.json fixture was introduced;
- no preview writer, provider, Skills, API, CLI, deployment, Desktop, Analyzer, or UI production surface changed.

- [ ] **Step 6: Perform a requirement-by-requirement completion audit.**

For every design requirement, identify its production implementation and focused test evidence. Treat missing or indirect evidence as incomplete.

- [ ] **Step 7: Finalize memory and stop.**

Update exact counts, close the session note, and stop after Repository Phase 30. Do not begin provider execution or any later work.

- [ ] **Step 8: Commit only when authorized.**

Proposed commit:

```text
feat(pbir): materialize modern PBIR safely
```
