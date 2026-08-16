# Phase 40 Advanced Chart Authoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add deterministic v5 chart authoring and reusable templates through a closed typed visual descriptor catalog while preserving v1–v4.

**Architecture:** Keep the existing provider, shared IR, Phase 29 serializer, Phase 31 materialization, and analyzer pipeline. Add v5-only models and adapters, resolve each visual through a static descriptor catalog, and use descriptor metadata for common role validation and projection. Extend serializer authoring output additively without changing prior-version paths.

**Tech Stack:** .NET 8, C#, xUnit, System.Text.Json, existing PBIR serializer/materialization/analyzer services.

---

### Task 1: Record Phase 40 scope and inspect baseline

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Create: `.agent-memory/sessions/20260814-phase40-advanced-chart-authoring.md`

- [ ] Record the approved v5 boundary, current HEAD, existing dirty paths, and the explicit no-commit/no-stage requirement.
- [ ] Record the baseline focused and full validation commands to run after implementation.

### Task 2: Add failing v5 contract and descriptor tests

**Files:**
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`
- Test: `service-dotnet/tests/Discovery/Phase40VisualDescriptorTests.cs`

- [ ] Add tests proving v5 exposes the six-visual catalog and static descriptors for roles, required roles, serializer mappings, and capabilities.
- [ ] Add tests for v5 template defaults, typed axis/legend/tooltip/conditional-formatting records, invalid role combinations, and unsupported authoring capabilities.
- [ ] Run the focused tests and confirm they fail because v5 models/descriptors are absent.

### Task 3: Implement typed v5 models and closed descriptor catalog

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationAuthoringModels.cs`
- Create: `service-dotnet/Services/Discovery/Models/Phase40VisualDescriptorModels.cs`
- Create: `service-dotnet/Services/Discovery/Phase40VisualDescriptorCatalog.cs`

- [ ] Add `LocalPbirGenerationRequestV5` and v5 contract constants without changing v1–v4 records.
- [ ] Add strongly typed template, axis, legend, tooltip, and deterministic conditional-formatting records.
- [ ] Define descriptor records for exactly Card, Table, Clustered Column Chart, Line Chart, Bar Chart, and Pie Chart.
- [ ] Implement catalog lookup and descriptor capability metadata with immutable ordered collections; do not add arbitrary registration or plugin APIs.
- [ ] Run the descriptor tests and confirm they pass.

### Task 4: Add v5 normalization and descriptor-driven validation

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] Add a v5 `Generate` overload and `GenerateAndVerifyAsync` overload that preserve the existing common pipeline.
- [ ] Adapt v5 to common generation inputs without routing v1–v4 through v5.
- [ ] Resolve descriptors through the static catalog and validate role cardinality, role/kind compatibility, visual-specific capabilities, template names, axis/legend/tooltip sections, and conditional rules.
- [ ] Replace chart-family mapping branches in the v5 path with descriptor-provided serializer role mappings; retain existing v1–v4 mapping code unchanged.
- [ ] Add failing-then-passing tests for every chart type, backward compatibility, invalid bindings, and v5 determinism.

### Task 5: Extend common authoring projection and serializer mapping

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs`
- Modify: `service-dotnet/Services/Discovery/Models/PbirDeployableSerializerModels.cs`
- Test: `service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs`

- [ ] Carry v5 resolved authoring into the existing serializer request as additive typed data.
- [ ] Emit deterministic axis, legend, tooltip, and supported conditional-formatting metadata in visual JSON using the locked schema-safe authoring representation.
- [ ] Extend structural validation for Bar, Pie, and Line role shapes while preserving existing Card/Table/Clustered Column output checks.
- [ ] Add serializer tests for exact role projections, stable property ordering, and schema-safe output.

### Task 6: Add round-trip, regression, and performance coverage

**Files:**
- Modify: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`
- Modify: `service-dotnet/tests/Discovery/PbirDeployableSerializerValidatorTests.cs`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase40-advanced-chart-authoring.md`

- [ ] Generate a representative multi-page v5 report using all chart families and multiple templates.
- [ ] Verify schema validation, materialization, analyzer scoring, lineage, repeated artifact hashes, and byte-identical files.
- [ ] Measure generation, materialization, and analyzer timings using the existing performance result contract.
- [ ] Document exact focused/full test counts, builds, known limitations, and measured observations.

### Task 7: Update authoritative documentation and close the session

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/phase39-generalized-visual-bindings-state.md`
- Create: `docs/current-state/phase40-advanced-chart-authoring-state.md`
- Create: `docs/pbir-visual-catalog-spec.md`
- Create: `docs/pbir-visual-template-spec.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Modify: `.agent-memory/sessions/20260814-phase40-advanced-chart-authoring.md`

- [ ] Document the six-visual catalog, binding matrix, template matrix, generated example, analyzer and determinism evidence, performance, limitations, and Phase 41 composition recommendation.
- [ ] Run `git diff --check`, confirm no staged files, and leave all Phase 40 changes uncommitted.

### Validation Commands

- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~LocalPbirGenerationProviderServiceTests|FullyQualifiedName~Phase40VisualDescriptorTests|FullyQualifiedName~PbirDeployableSerializerServiceTests"`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `dotnet build service-dotnet/PowerBIModelingService.sln -c Release`
- `cd vscode-extension && npm run compile && npm run build`
- `git diff --check`

