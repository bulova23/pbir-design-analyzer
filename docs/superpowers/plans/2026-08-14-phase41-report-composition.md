# Phase 41 Report Composition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add deterministic typed page composition, navigation, and slicer support through additive `local-pbir-generation-request/v6` while preserving v1–v5 behavior.

**Architecture:** Keep composition contract, projection, and validation in separate backend units. Project resolved composition into existing ordinary page/visual authoring and shared IR; retain serializer, materialization, analyzer, and scoring boundaries.

**Tech Stack:** .NET 8, C# records, xUnit, existing PBIR IR/serializer/materialization/analyzer.

---

### Task 1: Establish session memory and verify the baseline

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Create: `.agent-memory/sessions/20260814-phase41-report-composition.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Record the approved v6 scope, unchanged v1–v5 boundary, and the current validation baseline.
- [ ] Run `git status --short` and the focused Phase 40 provider/descriptor tests before modifying production code.

### Task 2: Add the failing v6 composition contract tests

**Files:**
- Modify: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`
- Create: `service-dotnet/tests/Discovery/Phase41CompositionTests.cs`

- [ ] Add tests asserting the v6 schema constant, four template catalog entries, typed section slots, and slicer descriptor presence.
- [ ] Add tests for valid representative composition, missing required slot, duplicate slot assignment, incompatible slot, invalid navigation target, and invalid slicer binding.
- [ ] Run the focused tests and confirm they fail because v6 records/catalog/projection do not yet exist.

### Task 3: Implement typed v6 contract records

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationAuthoringModels.cs`
- Create: `service-dotnet/Services/Discovery/Models/Phase41CompositionModels.cs`

- [ ] Add v6 schema constant and additive request record with nullable composition fields.
- [ ] Add closed enums/records for page templates, section kinds, slot assignments, navigation targets, slicer definitions, and slicer interactions.
- [ ] Keep v1–v5 declarations textually and semantically unchanged.
- [ ] Re-run contract tests to verify record construction and JSON property names.

### Task 4: Implement deterministic composition catalogs and projection

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase41CompositionCatalog.cs`
- Create: `service-dotnet/Services/Discovery/Phase41CompositionProjection.cs`
- Create: `service-dotnet/Services/Discovery/Phase41CompositionValidation.cs`
- Modify: `service-dotnet/Services/Discovery/Models/Phase40VisualDescriptorModels.cs`

- [ ] Define four immutable page-template catalogs with deterministic sections, slots, margins, and rectangles.
- [ ] Add the slicer descriptor with one Category/Dimension role and supported formatting capabilities.
- [ ] Implement validation for identifiers, page references, required/duplicate/unknown slots, compatibility, overflow, explicit-layout conflicts, navigation, and slicer bindings/interactions.
- [ ] Implement deterministic slot projection with explicit > slot > automatic precedence.
- [ ] Re-run focused tests and refactor only after green.

### Task 5: Integrate v6 into the provider without changing historical paths

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirIntermediateRepresentationService.cs` only if projection requires an existing IR input extension
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs` only for schema-proven slicer serialization
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs` only for descriptor-driven slicer validation

- [ ] Add `Generate` and `GenerateAndVerifyAsync` v6 overloads.
- [ ] Convert v6 to the existing generation input after composition projection; do not route v1–v5 through v6 defaults.
- [ ] Preserve existing materialization transaction and analyzer round-trip flow.
- [ ] Add only pinned-schema-supported slicer PBIR emission and validation.
- [ ] Run v6 provider round-trip tests and all prior provider/serializer/descriptor tests.

### Task 6: Add representative determinism and compatibility coverage

**Files:**
- Modify: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`
- Modify: `service-dotnet/tests/Discovery/PbirDeployableSerializerSchemaTests.cs` if schema fixtures require a focused gate
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase41-report-composition.md`

- [ ] Add the Executive Summary, Detail, and Comparison representative report with navigation and Region slicers.
- [ ] Assert artifact, manifest, file-set, lineage, and repeated-generation hashes.
- [ ] Assert analyzer `RoundTripVerified`, page/visual counts, and score/performance observations.
- [ ] Assert v1–v5 representative outputs remain unchanged against existing expectations.

### Task 7: Update specifications and current-state documentation

**Files:**
- Create: `docs/current-state/phase41-report-composition-state.md`
- Create: `docs/pbir-composition-spec.md`
- Create: `docs/pbir-page-template-spec.md`
- Modify: `docs/pbir-visual-catalog-spec.md`
- Modify: `docs/pbir-request-spec.md` if present, otherwise create the Phase 41 request-spec section in the current request documentation
- Modify: `docs/ROADMAP.md`

- [ ] Document v6, the composition model, template catalog, slicer scope, navigation, schema evidence, hashes, timings, compatibility, and limitations.
- [ ] Record that public RPC/VS Code exposure remains deferred and recommend the smallest Phase 42 capability based on contract stability.

### Task 8: Run repository-authoritative validation and close memory

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/sessions/20260814-phase41-report-composition.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Run focused Phase 41 tests, full Release backend tests, .NET build, extension Jest, webview Jest, TypeScript compilation, extension build, package, scoped lint, and `git diff --check`.
- [ ] Preserve and report the existing lint baseline and expected Windows skips.
- [ ] Record exact counts, timings, hashes, remaining limitations, and next recommendation.
- [ ] Confirm all Phase 41 changes remain uncommitted and unstaged.
