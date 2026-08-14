# Phase 42 Report Mutation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a narrow typed PBIR import and idempotent mutation pipeline without changing generation or public execution surfaces.

**Architecture:** Read pinned PBIR files into the existing shared IR, validate and resolve a closed mutation contract into an immutable deterministic plan, execute against an IR copy, then reuse the existing serializer, materialization, analyzer, hashing, and lineage pipeline. Import, planning, execution, and provider orchestration remain separate.

**Tech Stack:** .NET 8, C# records, `System.Text.Json`, xUnit, existing PBIR IR/serializer/materialization/analyzer services.

---

### Task 1: Add typed mutation and import contracts

**Files:**
- Create: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs`
- Create: `service-dotnet/Services/Discovery/Models/PbirLocalReportImportModels.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirMutationContractTests.cs`

- [ ] Define `local-pbir-mutation-request/v1`, closed operation records, selectors, diagnostics, plan, evidence, result, and import snapshot records. Keep all types `internal` and use existing naming/JSON conventions.
- [ ] Add tests asserting the schema versions, operation discriminators, required fields, and generation model declarations remain unchanged.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirMutationContractTests`; expect initial failures until the contracts exist, then pass.

### Task 2: Implement the narrow local PBIR reader

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirLocalReportReader.cs`
- Create: `service-dotnet/Services/Discovery/PbirLocalReportReaderJson.cs`
- Test: `service-dotnet/tests/Discovery/PbirLocalReportReaderTests.cs`

- [ ] Write fixture-free tests that materialize a representative Phase 41 artifact to a temporary directory, import it, assert page/visual identity and supported binding projection, and reject missing files, wrong schemas, duplicate identities, unsupported visual shapes, and ambiguous references.
- [ ] Implement explicit file inventory and pinned schema checks, using `System.Text.Json` only for the known report/page/visual shapes.
- [ ] Project accepted data into `PbirIntermediateRepresentation` and validate it with the existing IR validator/readiness services; compute source hashes and return a typed snapshot without writing files.
- [ ] Run the focused reader tests and the existing IR/serializer regression tests.

### Task 3: Add deterministic mutation validation and planning

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirMutationPlanner.cs`
- Create: `service-dotnet/Services/Discovery/PbirMutationPlanning.cs`
- Test: `service-dotnet/tests/Discovery/PbirMutationPlannerTests.cs`

- [ ] Add red tests for target resolution, duplicate IDs, missing/ambiguous targets, incompatible replacement, slot/layout conflicts, navigation conflicts, and deterministic operation ordering.
- [ ] Implement full-request validation and immutable snapshot resolution before execution or serialization.
- [ ] Implement no-op recognition for already-satisfied operations and mutation fingerprint validation for reused mutation IDs.
- [ ] Run focused planner tests and confirm no filesystem output is created during planning.

### Task 4: Execute mutations through shared IR

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirMutationExecutor.cs`
- Create: `service-dotnet/Services/Discovery/PbirMutationIntegrity.cs`
- Test: `service-dotnet/tests/Discovery/PbirMutationExecutorTests.cs`

- [ ] Add red tests for add/remove/rename/move pages, add/remove/replace/move/resize visuals, binding/formatting/filter/theme/navigation/slicer updates, stable identity, unchanged layout, and hash differences limited to affected artifacts.
- [ ] Implement record-based IR transformations with deterministic ordering and explicit identity allocation; reject operations not representable by the current IR/serializer.
- [ ] Recompute IR content/lineage hashes using existing integrity conventions and preserve imported lineage entries.
- [ ] Run focused executor tests and serializer schema tests.

### Task 5: Orchestrate mutation and mandatory round-trip evidence

**Files:**
- Create: `service-dotnet/Services/Discovery/LocalPbirMutationProviderService.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs` only if shared round-trip result types are required
- Test: `service-dotnet/tests/Discovery/LocalPbirMutationProviderServiceTests.cs`

- [ ] Add red tests for successful mutation, rejected import/plan, analyzer regression, no-change reapplication, deterministic repeated execution, and generation compatibility.
- [ ] Implement provider orchestration: import → plan → execute → existing serializer → schema validation → existing materialization/analyzer path → structured evidence.
- [ ] Keep source generation overloads untouched and do not register a route, RPC method, VS Code command, or provider activation.
- [ ] Add measured phase timings and preserved/changed identity/hash evidence.

### Task 6: Document operation matrix, examples, limitations, and benchmark

**Files:**
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase42-report-mutation.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/pbir-request-spec.md`
- Create or modify: `docs/architecture/phase42-report-mutation.md`
- Test: documentation scans as applicable

- [ ] Document the supported operation matrix, representative request/result, analyzer before/after score, identity/hash behavior, measured timings, limitations, and Phase 43 minimal-RPC recommendation.
- [ ] State explicitly that mutation is backend-only and that unsupported constructs fail closed.
- [ ] Run documentation/reference scans and `git diff --check`.

### Task 7: Run the full validation matrix and close session memory

**Files:**
- Create: `.agent-memory/sessions/20260814-phase42-report-mutation.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Run focused mutation tests, analyzer/schema regression, full Release backend tests, .NET Release build, extension build/TypeScript/tests, and `git diff --check`.
- [ ] Record exact counts, expected Windows skips, timing observations, any unchanged lint baseline, and validation gaps.
- [ ] Verify `git status --short` shows all Phase 42 files uncommitted and unstaged.
