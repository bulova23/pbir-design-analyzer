# Phase 37 — Incremental PBIR Authoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a deterministic backend-only PBIR authoring contract for multiple pages, `card` and `table` visuals, typed bindings, and bounded layout while preserving Phase 36 requests.

**Architecture:** Keep `LocalPbirGenerationProviderService` as the only provider. Normalize v1 and v2 typed requests into one private authoring model, map that model into the existing Phase 29 IR/deployable serializer requests, then reuse Phase 31 materialization and the existing analyzer. Do not modify RPC, VS Code, provider security, serializer architecture, or semantic-model generation.

**Tech Stack:** .NET 8, C# records, existing PBIR IR and deployable serializer, Phase 31 orchestration, `PbirProjectService`, `PbirScoringService`, xUnit.

---

## File map

- Modify `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs` with additive v2 records, binding/layout records, and visual catalog constants.
- Modify `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs` to normalize v1/v2, validate collections/layout/bindings, map all pages/visuals, and verify requested counts.
- Modify `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs` with focused red-green coverage and timing observations.
- Modify `docs/ROADMAP.md`, `docs/current-state/generation-provider-framework-state.md`, and `docs/current-state/reference-generator-state.md` to describe Phase 37.
- Create `docs/superpowers/implementation-notes/2026-08-13-phase37-incremental-pbir-authoring.md` with the request, generated artifact inventory, analyzer result, hashes, timings, tests, and limitations.
- Create and finalize `.agent-memory/sessions/20260813-phase37-incremental-pbir-authoring.md`; update `.agent-memory/current-focus.md` and `.agent-memory/session-summaries.md`.

### Task 1: Add typed v2 contract and compatibility normalization tests

**Files:**
- Modify `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- Test `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Write failing tests asserting `card` and `table` are the only provider visual types, v2 pages/visuals/bindings/layout are strongly typed, v1 normalizes to one page/one card/one measure, and v2 rejects empty collections.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirGenerationProviderServiceTests`; verify failure is caused by missing v2 types/normalization.
- [ ] Add `LocalPbirGenerationRequestV2`, `LocalPbirGenerationPage`, `LocalPbirGenerationVisual`, `LocalPbirGenerationBinding`, `LocalPbirGenerationLayout`, and enums/constants. Keep the existing v1 record unchanged.
- [ ] Add an internal normalization result used only by the provider; do not expose a JSON blob or runtime provider interface.
- [ ] Re-run the focused tests and verify the contract/normalization assertions pass.

### Task 2: Add failing validation tests for identity, references, bindings, and layout

**Files:**
- Test `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add tests for duplicate page ids, duplicate visual ids, duplicate binding ids, visual page references that do not exist, unsupported visual type, unsupported binding kind, card dimensions, empty table bindings, and missing binding semantic fields.
- [ ] Add tests for negative coordinates, zero sizes, out-of-canvas bounds, overlapping visuals, and omitted layout values that must be deterministically auto-placed.
- [ ] Assert every failure returns `Rejected` with no artifact or manifest and stable `PBIR37-*` diagnostic codes.
- [ ] Run the focused test filter and verify the new tests fail before implementation.

### Task 3: Implement minimal normalization and request validation

**Files:**
- Modify `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`

- [ ] Normalize v1 to the existing Phase 36 identity/layout values and v2 to ordered internal collections. Reject unsupported schema versions before constructing IR.
- [ ] Validate identifiers with the existing safe identifier rules; validate collection uniqueness and cross-references using ordinal comparisons.
- [ ] Validate visual/binding combinations: card requires measures only; table accepts measures/dimensions and requires at least one binding; only direct fields are allowed.
- [ ] Validate layout against the fixed `1280x720` canvas, positive dimensions, and non-overlap. Assign omitted positions using deterministic row-major 8px-grid placement ordered by `(pageOrder, visualOrder, visualId)`.
- [ ] Re-run focused validation tests and verify all new failure cases pass while Phase 36 tests remain green.

### Task 4: Add failing multi-page/multi-visual artifact tests

**Files:**
- Test `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add a representative v2 request with two ordered pages: an overview page containing a card and table, and a detail page containing a table with one dimension and two measures.
- [ ] Assert generated files include both pages and all visuals, page order is stable, visual files contain `card` or `table`, and table projections contain both measure and dimension fields.
- [ ] Assert semantic-model inventory contains every referenced field exactly once and visual bindings reference the correct inventory entries.
- [ ] Run the focused test filter and verify these tests fail because `CreateInputs` still maps only the Phase 36 singleton.

### Task 5: Implement collection-to-IR and serializer mapping

**Files:**
- Modify `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`

- [ ] Replace singleton input construction with collection mapping while preserving the existing Phase 36 v1 identity derivation and six-file output expectations.
- [ ] Map each page to a deterministic IR page order and each visual to a deterministic IR visual order; derive page transitions from adjacent ordered pages and use the first page as landing page.
- [ ] Create per-page semantics from referenced measures/dimensions and create one layout container per page with visual references in visual order.
- [ ] Build a sorted semantic-model inventory and `PbirDeployableVisualBinding` list from typed bindings; use `Fields` role and one-based projection order.
- [ ] Preserve the existing serializer request, schema locks, `modernPbir`, `modern-grid-1280x720/v1`, no-authority policy, lineage, and canonical hash delegation.
- [ ] Run focused artifact tests and verify the representative multi-page report serializes successfully.

### Task 6: Add analyzer round-trip, determinism, and performance tests

**Files:**
- Test `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`
- Modify `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs` only if count verification needs generalization.

- [ ] Generalize round-trip verification to compare analyzer page count and generated visual count with the normalized request counts, and report stable `PBIR37-ROUNDTRIP-*` diagnostics.
- [ ] Add an async two-page round-trip test asserting `RoundTripVerified`, materialization applied/exact-match, analyzer page count two, and visual count four or more for the representative request.
- [ ] Add repeated generation assertions for file bytes, per-file hashes, artifact hashes, manifest hashes, file-set hash, and lineage hash.
- [ ] Add a timing test using `Stopwatch` for generation, materialization, and analyzer; assert only that all phases complete and write the observed values to test output or the implementation note.
- [ ] Run the focused tests and the existing Phase 29 serializer/analyzer regression filters.

### Task 7: Update documentation and session memory

**Files:**
- Modify `docs/ROADMAP.md`
- Modify `docs/current-state/generation-provider-framework-state.md`
- Modify `docs/current-state/reference-generator-state.md`
- Create `docs/superpowers/implementation-notes/2026-08-13-phase37-incremental-pbir-authoring.md`
- Create `.agent-memory/sessions/20260813-phase37-incremental-pbir-authoring.md`
- Modify `.agent-memory/current-focus.md` and `.agent-memory/session-summaries.md`

- [ ] Document the supported matrix: multiple pages, `card`, `table`, measure/dimension Fields bindings, explicit/auto layout, and analyzer round-trip.
- [ ] Document generated example paths, representative score/page/visual counts, deterministic hash comparison, and measured timings from the validated run.
- [ ] Document unsupported chart semantics, filters, formatting, themes, interactions, semantic-model generation, RPC, VS Code, hosted/Windows execution, and custom visuals.
- [ ] Record that the Phase 35 provider catalog/security conclusions are unchanged and the worktree remains intentionally uncommitted/unstaged.

### Task 8: Run full validation and close out without commit

**Files:**
- No source changes expected; inspect all modified files.

- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release` and record exact counts/outcomes, preserving expected Windows skips.
- [ ] Run `dotnet build service-dotnet/PbirDesignAnalyzer.Core.csproj -c Release`.
- [ ] Run `cd vscode-extension && npm run build`.
- [ ] Run `git diff --check`.
- [ ] Inspect `git diff --stat`, `git diff --name-only`, and `git status --short`; verify no unrelated dirty files were changed and no files are staged or committed.
- [ ] Finalize the session note and current focus with any validation limitations and the Phase 38 recommendation: richer formatting/filters/interactions/themes before charts or public surfaces.

