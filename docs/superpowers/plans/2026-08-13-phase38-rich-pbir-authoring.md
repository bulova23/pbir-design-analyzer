# Phase 38 Rich PBIR Authoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add typed, deterministic formatting, themes, filters, interactions, layout enhancements, and metadata to the backend-only local PBIR generation provider without changing v1/v2 behavior.

**Architecture:** Add an internal v3 request and typed authoring model, normalize it through the existing provider/IR boundary, and extend the existing serializer writer and validator for only the pinned PBIR schema properties. Keep materialization and analyzer round-trip unchanged except for v3 count/metadata checks.

**Tech Stack:** .NET 8, C# records/enums, xUnit, pinned offline JSON schemas, existing Phase 29 serializer and Phase 31 materialization services.

---

### Task 1: Add typed v3 authoring contracts

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- Create: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationAuthoringModels.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add v3 schema constant, typed formatting/theme/filter/interaction/layout/metadata records, and v3 request.
- [ ] Add contract tests for supported visual catalog and typed field defaults.
- [ ] Run the focused provider test to establish the expected failure.

### Task 2: Validate v3 authoring inputs

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add fail-closed validation for colors, dimensions, alignment, formats, filters, duplicate themes, conflicts, and unsupported interaction matrices.
- [ ] Add tests for every invalid/unsupported case.
- [ ] Run focused tests.

### Task 3: Carry typed authoring through the shared serializer request

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/PbirDeployableSerializerModels.cs`
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerSafetyGate.cs`
- Test: `service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs`

- [ ] Add an optional typed authoring payload to the existing internal serializer request with deterministic defaults.
- [ ] Normalize v3 pages, visuals, bindings, inventory, and authoring values without changing v1/v2 construction.
- [ ] Add serializer input and hash tests.

### Task 4: Emit schema-valid report, page, and visual authoring

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs`
- Test: `service-dotnet/tests/Discovery/PbirDeployableSerializerSchemaTests.cs`

- [ ] Emit report metadata, themes, report/page filters, page formatting, visual filters, card/table formatting, numeric formats, and supported interaction settings.
- [ ] Keep property and collection ordering deterministic.
- [ ] Extend exact-template validation for optional schema-approved properties.
- [ ] Run pinned schema validation and serializer tests.

### Task 5: Add v3 provider generation and round-trip coverage

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add `Generate` and `GenerateAndVerifyAsync` overloads for v3.
- [ ] Assert representative formatted multi-page report output, analyzer score, and performance timings.
- [ ] Assert repeated v3 generation has identical files and hashes.
- [ ] Run focused provider, serializer, and analyzer regression tests.

### Task 6: Update documentation and session memory

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/generation-provider-framework-state.md`
- Create: `docs/current-state/phase38-rich-pbir-authoring-state.md`
- Create: `docs/pbir-rich-authoring-formatting-spec.md`
- Modify: `.agent-memory/current-focus.md`
- Create: `.agent-memory/sessions/2026-08-13-phase38-rich-pbir-authoring.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Document the formatting matrix, generated example, analyzer results, determinism, performance observations, test counts, limitations, and Phase 39 recommendation.
- [ ] Preserve repository-local fixture details and record validation outcomes.

### Task 7: Full validation and worktree handoff

- [ ] Run focused provider/serializer tests.
- [ ] Run schema validation and analyzer regression.
- [ ] Run full backend suite and .NET build.
- [ ] Run TypeScript compilation, extension build, and `git diff --check`.
- [ ] Confirm Phase 38 files are modified but unstaged/uncommitted and preserve unrelated worktree changes.
