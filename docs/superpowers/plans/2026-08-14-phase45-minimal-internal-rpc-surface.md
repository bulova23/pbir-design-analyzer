# Phase 45 — Superseded RPC Proposal

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Status:** Superseded by the Phase 45 architecture decision recorded in `docs/current-state/phase45-roadmap-reconciliation.md` and `docs/superpowers/specs/2026-08-14-phase45-minimal-internal-rpc-surface-design.md`.

**Goal:** Historical proposal only. It is not an implementation authorization or an executable Phase 45 plan.

**Architecture:** The proposal described a five-operation transport-independent adapter. Option B was selected instead: retain the existing typed backend services and do not register or implement this adapter.

**Tech Stack:** .NET 8, C# records/enums, xUnit, existing PBIR authoring services. No new RPC surface is approved.

> Do not execute the task list below. It is retained to explain the superseded proposal and the scope that was intentionally rejected.

---

### Task 1: Record the approved design and establish session memory

**Files:**
- Create: `docs/superpowers/specs/2026-08-14-phase45-minimal-internal-rpc-surface-design.md`
- Create: `docs/superpowers/plans/2026-08-14-phase45-minimal-internal-rpc-surface.md`
- Create: `.agent-memory/sessions/20260814-phase45-minimal-internal-rpc-surface.md`
- Modify: `.agent-memory/current-focus.md`

- [x] **Step 1: Write the approved design and plan**

Use the checked-in-but-uncommitted design and plan files. Preserve the independent RPC version, v1–v7 generation compatibility, opaque snapshot boundary, and all explicit non-goals.

- [x] **Step 2: Record the active session**

Record that implementation is authorized, all changes must remain unstaged/uncommitted, and the next action is contract-first TDD.

### Task 2: Define the transport-independent typed contract

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcContract.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcContractTests.cs`

- [ ] **Step 1: Write failing contract-shape tests**

Assert that the RPC schema constant is `pbir-authoring-rpc/v1`; the operation catalog contains exactly Generate, Import, Mutate, Validate, and Analyze; error categories are closed; the generation union has exactly v1–v7 cases; snapshot handles are versioned and contain no IR/file-content fields; and result records expose diagnostics, timing, artifact identity, analyzer summary, and optional fidelity without generic dictionaries.

- [ ] **Step 2: Run the focused test and verify the expected missing-type failure**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcContractTests`. Expected: compilation failure because the new contract namespace/types do not yet exist.

- [ ] **Step 3: Implement the minimal contract**

Define internal records/enums with JSON property names, typed operation payloads, a typed `PbirAuthoringGenerationRequest` union for existing request records v1–v7, `PbirAuthoringSnapshotHandle`, `PbirAuthoringArtifactIdentity`, `PbirAuthoringFidelity`, `PbirAuthoringTiming`, `PbirAuthoringDiagnostic`, `PbirAuthoringRpcError`, and typed operation result payloads. Do not copy existing generation or mutation record fields into new schema versions.

- [ ] **Step 4: Run the focused test and verify it passes**

Run the same focused command. Expected: all contract-shape tests pass.

### Task 3: Add dispatcher seam and invalid-request behavior

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcDispatcherTests.cs`

- [ ] **Step 1: Write failing dispatcher tests**

Cover null requests, wrong RPC version, missing operation payload, unknown operation, malformed generation union, and deterministic safe error mapping with no exception text.

- [ ] **Step 2: Run focused tests and verify they fail because the dispatcher is absent**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcDispatcherTests`. Expected: compilation failure for the missing dispatcher.

- [ ] **Step 3: Implement shared dispatch validation and safe diagnostics**

Create one dispatcher entry point returning a typed response. Validate the independent RPC version and exactly one closed operation payload. Use stable summaries and error categories; catch known `IOException`, `UnauthorizedAccessException`, `InvalidDataException`, `ArgumentException`, and analyzer failures into stable categories, and map all unexpected failures to `InternalFailure` without returning exception messages.

- [ ] **Step 4: Run focused tests and verify invalid requests pass**

Run the focused dispatcher command and confirm all malformed-request tests pass.

### Task 4: Implement Generate over the existing v1–v7 provider overloads

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcGenerationTests.cs`

- [ ] **Step 1: Write failing generation tests**

For each existing generation request v1–v7, invoke the dispatcher using the same typed request used by the provider tests and assert a generated/round-trip-verified result, stable artifact identity, timings, diagnostics, and analyzer summary. Run the same request twice and compare artifact hashes and analyzer projections.

- [ ] **Step 2: Run the focused generation tests and verify failure before delegation exists**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcGenerationTests`. Expected: failing assertions or missing delegation implementation.

- [ ] **Step 3: Delegate each union case to `LocalPbirGenerationProviderService`**

Use explicit pattern matching for v1–v7; never normalize into a new generation contract. Project only stable result fields into RPC result records, copy artifact/manifest identity and fidelity where existing result data provides it, and capture dispatch/orchestration/serialization/analyzer elapsed observations from existing performance data plus dispatcher timing.

- [ ] **Step 4: Run focused generation tests and verify determinism/equivalence**

Run the focused command and compare direct provider artifact hashes and analyzer summaries with dispatcher results.

### Task 5: Implement Import with an opaque snapshot handle

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcImportTests.cs`

- [ ] **Step 1: Write failing import tests**

Import a repository-owned generated PBIR directory, assert a versioned opaque handle and artifact/source identity, assert analyzer/diagnostic summary behavior, reject missing/non-PBIR directories, and assert serialized response shape contains no IR, raw JSON, or arbitrary file map.

- [ ] **Step 2: Run focused tests and verify failure**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcImportTests`. Expected: failure because import dispatch/handle storage is absent.

- [ ] **Step 3: Delegate to `PbirLocalReportReader` through the existing mutation provider and store only private snapshot state**

Generate a deterministic opaque handle from the imported source identity and normalized snapshot identity. Keep the actual `PbirLocalReportImportSnapshot` in a dispatcher-owned private store keyed by the handle. Return only the typed handle and identity/diagnostic metadata.

- [ ] **Step 4: Run focused import tests and verify safe boundary behavior**

Run the focused command and confirm valid imports return handles while invalid paths return `ImportFailed` without raw filesystem details beyond bounded field summaries.

### Task 6: Implement Mutate through planner, executor, merge, serializer, validation, and analyzer

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcMutationTests.cs`

- [ ] **Step 1: Write failing mutation tests**

Use an imported handle and `local-pbir-mutation-request/v1` to test a supported resize/rename/add operation, a mutation conflict, an unsupported authoring operation, stale/unknown handles, deterministic result hashes, fidelity classification, and direct pipeline analyzer equivalence.

- [ ] **Step 2: Run focused tests and verify failure**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcMutationTests`. Expected: failure before mutation delegation is present.

- [ ] **Step 3: Compose the existing mutation path without shortcuts**

Resolve and validate the opaque handle, call the existing mutation provider planner and executor, call `PbirAuthoringMergeService`, construct the same serializer/materialization input used by the existing Phase 43/44 tests, validate output with `PbirDeployableSerializerValidator`, compute fidelity with `PbirRoundTripFidelityService`, and analyze the materialized report through `PbirScoringService`. Map planner conflict diagnostics to `MutationConflict` and unsupported authoring diagnostics to `UnsupportedAuthoring`.

- [ ] **Step 4: Run focused mutation tests and verify fidelity/equivalence**

Run the focused command; compare direct pipeline and RPC artifact hashes, fidelity classifications, and analyzer summaries.

### Task 7: Implement Validate and Analyze

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcValidationAndAnalysisTests.cs`

- [ ] **Step 1: Write failing validation/analyzer tests**

Cover valid generated artifacts, schema-invalid artifacts, supported report analysis, analyzer failure mapping, and direct analyzer versus RPC result equivalence.

- [ ] **Step 2: Run focused tests and verify failure**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcValidationAndAnalysisTests`. Expected: failure before operation handlers exist.

- [ ] **Step 3: Delegate to existing schema and analyzer services**

Validate typed artifact/manifest inputs using the existing serializer validator and return only typed validation diagnostics. Analyze only typed admitted report references, call `PbirScoringService`, return `ScoreResult`, and classify failures as `AnalyzerFailed`.

- [ ] **Step 4: Run focused tests and verify analyzer consistency**

Run the focused command and assert exact direct/RPC score projections for representative reports.

### Task 8: Complete documentation and repository memory

**Files:**
- Create: `docs/current-state/pbir-authoring-rpc-state.md`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase45-minimal-internal-rpc-surface.md`
- Modify: `docs/ROADMAP.md`
- Modify: relevant backend architecture/current-state documentation
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Modify: `.agent-memory/sessions/20260814-phase45-minimal-internal-rpc-surface.md`

- [ ] **Step 1: Document operation, error, and compatibility matrices**

List exactly five operations, their typed inputs/outputs, existing service delegation, error categories, generation v1–v7 compatibility, mutation v1 compatibility, and the opaque snapshot boundary.

- [ ] **Step 2: Record direct-vs-RPC equivalence and timing observations**

Record artifact hashes, analyzer equality, dispatch/orchestration/serialization/analyzer elapsed observations, and explain that timings are representative observations with no thresholds.

- [ ] **Step 3: Record limitations and Phase 46 recommendation**

State that transport registration, VS Code mutation, streaming, auth/authz, hosted/Windows/Desktop execution, semantic-model/DAX generation, and other deferred authoring domains remain out of scope. Recommend only Generate, Import, and Analyze for a future VS Code integration.

### Task 9: Run the complete validation matrix without staging or committing

**Files:**
- Test/verify: repository outputs and all changed files

- [ ] **Step 1: Run focused RPC tests and relevant regressions**

Run the Phase 45 focused tests, generation provider tests, mutation/reader/serializer/fidelity/analyzer regression tests, and offline schema validation tests.

- [ ] **Step 2: Run the full backend suite and .NET build**

Run `dotnet test service-dotnet/tests/Tests.csproj -c Release` and `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release`. Preserve existing expected Windows skips and report exact counts.

- [ ] **Step 3: Verify extension non-regression**

Run the repository-required extension TypeScript compilation/build/test commands without changing extension files.

- [ ] **Step 4: Run whitespace and scope checks**

Run `git diff --check`, inspect `git status --short`, verify no `RpcHost` or VS Code modifications, and confirm all Phase 45 changes are unstaged and uncommitted.
