# Phase 39 Generalized Visual Bindings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an additive v4 local PBIR request with typed visual binding roles and a clustered column chart while preserving v1–v3 behavior and the existing serializer/materialization/analyzer path.

**Architecture:** Keep v1–v3 records and overloads unchanged. Add a small typed binding vocabulary to the v4 request and shared IR, normalize role bindings once in the provider, and map normalized bindings to the existing Phase 29 serializer projections. Reuse deterministic layout, formatting, schema validation, materialization, and analyzer round-trip.

**Tech Stack:** .NET 8, C# records/enums, xUnit, existing PBIR serializer and materialization services, Markdown documentation.

---

### Task 1: Establish the v4 contract and red tests

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationAuthoringModels.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add `SchemaVersionV4`, `clusteredColumnChart` to the provider allowlist, strongly typed `LocalPbirGenerationBindingRole` and `LocalPbirGenerationBindingKind` values needed by the v4 model, a role-bearing binding record, v4 visual/request records, and minimal chart formatting records.
- [ ] Add failing tests for a v4 card/table/chart request, duplicate roles, invalid role-kind combinations, and unsupported roles.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirGenerationProviderServiceTests`; confirm the new tests fail because v4 is not implemented.

### Task 2: Normalize generalized bindings into the shared IR

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs`
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add an additive optional binding collection to the IR visual plus typed IR binding/role records; keep existing constructor call sites source-compatible through optional trailing data.
- [ ] Add v4 validation for required roles, one-based contiguous projection order within each role, duplicate role/cardinality violations, field-kind compatibility, page/visual/reference/path/layout rules, and the existing chart-only capability boundary.
- [ ] Build the semantic inventory, IR, semantic records, and serializer request from one normalized binding list; preserve deterministic ordering and hashes.
- [ ] Run the focused tests and verify valid v4 normalization passes while invalid bindings reject before artifact creation.

### Task 3: Map normalized roles through the existing serializer

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Map card Value to the existing `Fields` role, table Value/Category-compatible fields to `Values`, and chart Category/Value to the existing `Category`/`Y` serializer roles without changing v1–v3 output.
- [ ] Extend serializer validation to reject duplicate role projections, unsupported role combinations, and non-contiguous role orders with deterministic diagnostics.
- [ ] Add chart formatting serialization for title, axis labels, legend visibility, background, and palette colors using the existing `objects` shape.
- [ ] Add tests that inspect emitted chart JSON, serializer role ordering, and regression byte equality for representative Phase 38 requests.

### Task 4: Add v4 generation and analyzer round-trip coverage

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add `Generate` and `GenerateAndVerifyAsync` overloads for v4 using the same Phase 31 preview/apply and analyzer scoring flow, with a phase39 transaction prefix and count checks.
- [ ] Add a representative multi-page report with Card, Table, and Clustered Column Chart; assert schema validation, lineage, analyzer round-trip, visual count, deterministic artifact/file hashes, and timings.
- [ ] Add invalid-binding tests for duplicate roles, unsupported role combinations, missing category/value, wrong field kinds, duplicate binding ids, and non-contiguous role order.
- [ ] Run the focused provider suite and the full backend suite.

### Task 5: Document the binding model and current state

**Files:**
- Create: `docs/superpowers/specs/2026-08-14-phase39-generalized-visual-bindings-design.md`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase39-generalized-visual-bindings.md`
- Create: `docs/current-state/phase39-generalized-visual-bindings-state.md`
- Modify: `docs/ROADMAP.md`

- [ ] Document the executive summary, binding matrix, visual catalog, generated example, analyzer and determinism results, performance observations, tests, limitations, and Phase 40 recommendation.
- [ ] Explicitly state that v1–v3 remain supported, chart support is advisory/backend-only, no RPC/VS Code/hosted/Windows surface was added, and advanced chart capabilities are deferred.

### Task 6: Final verification and session closeout

**Files:**
- Create: `.agent-memory/sessions/20260814-phase39-generalized-visual-bindings.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Run focused provider tests, generalized binding/chart tests, backend Release tests, .NET build, extension build/TypeScript compilation, and `git diff --check`.
- [ ] Record exact counts, expected Windows skips, analyzer score, hashes, and performance observations; record any validation limitation instead of inferring success.
- [ ] Verify the Phase 39 diff is unstaged and uncommitted and preserve unrelated worktree changes.
