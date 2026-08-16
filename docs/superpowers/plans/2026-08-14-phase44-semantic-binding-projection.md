# Phase 44 Semantic Binding Projection and Full Round-Trip Fidelity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Project imported PBIR query-state bindings through the existing descriptor catalog into the shared IR, preserve unsupported semantics through the Phase 43 envelope with diagnostics, and produce analyzer/fidelity/timing evidence for safe round trips and mutations.

**Architecture:** Keep `PbirIntermediateRepresentationBinding` as the only semantic binding model. Add narrowly scoped descriptor metadata and a projection method to the existing catalog, then make `PbirLocalReportReader` use it before constructing IR. Keep envelope preservation and serializer merge behavior unchanged except where evidence proves a symmetry fix is required. Add evidence as additive internal contracts and use the existing analyzer, mutation, fidelity, and serializer services.

**Tech Stack:** .NET 8, C#, xUnit, `System.Text.Json`, existing PBIR schema lock, Phase 40/41 descriptor catalogs, Phase 43 authoring envelope/merge/fidelity services.

---

### Task 1: Establish the descriptor projection contract with failing tests

**Files:**
- Modify: `service-dotnet/tests/Discovery/Phase40VisualDescriptorTests.cs`
- Modify: `service-dotnet/tests/Discovery/PbirLocalReportReaderTests.cs`
- Create: `service-dotnet/tests/Discovery/Phase44SemanticProjectionTests.cs`

- [ ] **Step 1: Write tests for the existing shared binding projection contract**

Add tests that construct generated PBIR for every supported family, import it, and assert that the imported `PbirIntermediateRepresentationVisual.Bindings` contain the same role, kind, entity, property, token, and projection order as the generated `LocalPbirGenerationBinding` input. Include Card, Table, Clustered Column Chart, Line Chart with Series, Bar Chart, Pie Chart with Legend, and Slicer.

Add a catalog test that asserts every supported visual has a descriptor import mapping and that the mapped canonical roles are members of `LocalPbirGenerationBindingRole`.

- [ ] **Step 2: Run the focused tests and verify the expected red failure**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~Phase44SemanticProjectionTests|FullyQualifiedName~Phase40VisualDescriptorTests|FullyQualifiedName~PbirLocalReportReaderTests"
```

Expected: the new projection assertions fail because the reader currently uses raw query-state role names and does not resolve them through descriptors, while the generated chart/slicer fixtures expose role mappings that are not yet imported consistently.

- [ ] **Step 3: Keep the test fixture builders deterministic**

Reuse the existing generated-artifact writing helpers and fixed UTC timestamps. Do not add external PBIR fixtures or random semantic tokens. Use temporary directories with `finally` cleanup as the current reader tests do.

### Task 2: Extend the existing descriptor catalogs narrowly

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/Phase40VisualDescriptorModels.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs` only if a shared enum/type must be moved or exposed without changing its serialized contract
- Modify: `service-dotnet/tests/Discovery/Phase40VisualDescriptorTests.cs`

- [ ] **Step 1: Add import aliases to the existing descriptor role projection records**

Extend `Phase40VisualRoleProjection` with an immutable list of imported PBIR role aliases, defaulting to the existing serializer role. Preserve all current constructor call sites by adding the new property as an optional trailing value or update the single catalog factory in one change.

Use repository evidence for aliases: `Fields` and `Values` map to `Value`; `Category` maps to `Category`; `Series` maps to `Series`; `Legend` maps to `Legend`; `Tooltip` maps to `Tooltip`; `Y` maps to `Value`; and Pie `Category` maps to `Legend` only where the descriptor already declares the canonical pie role as `Legend`. Do not infer aliases for roles absent from a descriptor.

- [ ] **Step 2: Add one catalog lookup that resolves a visual type and imported role**

Implement a method on `Phase40VisualDescriptorCatalog` that returns the unique `Phase40VisualRoleProjection` for `(visualType, importedRole)`, or an explicit result indicating unknown/unsupported/ambiguous. Do not return a generation binding or create another role model. Ensure slicer resolution delegates to the existing Phase 41 descriptor catalog and only accepts its Category role.

- [ ] **Step 3: Run catalog tests**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase40VisualDescriptorTests
```

Expected: all catalog tests pass, including the new alias and supported-family assertions.

### Task 3: Implement descriptor-based reader projection and structured diagnostics

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirLocalReportReader.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs`
- Modify: `service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs` only if projection status must be carried in the existing IR validation/evidence contract
- Modify: `service-dotnet/tests/Discovery/PbirLocalReportReaderTests.cs`
- Modify: `service-dotnet/tests/Discovery/Phase44SemanticProjectionTests.cs`

- [ ] **Step 1: Add failing diagnostics tests**

Add tests for a schema-admitted visual containing an unknown query-state role and a known descriptor role with an invalid field shape. Assert that the original visual envelope item still contains the source JSON, the unsupported role is absent from typed bindings, and diagnostics identify the visual, role, and outcome (`PreservedButUntyped` or `Invalid`). Add a test for an invalid measure/dimension combination against a descriptor role kind.

- [ ] **Step 2: Replace raw role parsing with descriptor resolution**

Update `PbirLocalReportReader.ReadBindings` to receive the visual type, resolve each query-state role through `Phase40VisualDescriptorCatalog`, and project only resolved roles into `PbirIntermediateRepresentationBinding`. Preserve `projectionOrder`, entity, property, and the strongest available token/reference. For a resolved alias, use the descriptor’s canonical `LocalPbirGenerationBindingRole` converted to the existing IR enum.

Use diagnostics with stable Phase 44 codes, for example:

```text
PBIR44-IMPORT-ROLE-001 unknown query-state role
PBIR44-IMPORT-ROLE-002 unsupported descriptor role
PBIR44-IMPORT-ROLE-003 ambiguous role mapping
PBIR44-IMPORT-BINDING-001 invalid field shape
PBIR44-IMPORT-BINDING-002 descriptor kind conflict
```

Unknown or unsupported roles must not be converted to `Value`. Invalid projections must not be emitted as typed bindings. The already-read source visual must remain envelope-preserved; no raw JSON mutation path may be introduced.

- [ ] **Step 3: Make invalid imported typed semantics block readiness without destroying the envelope**

When a supported visual has a descriptor-required role with invalid shape or a conflicting duplicate mapping, retain the envelope and return a blocked import state with the structured diagnostic. A merely unsupported optional role remains importable with `PreservedButUntyped` diagnostics if all typed required roles are valid.

- [ ] **Step 4: Run the focused reader tests**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirLocalReportReaderTests|FullyQualifiedName~Phase44SemanticProjectionTests"
```

Expected: supported projections pass, unsupported-role tests show envelope preservation and diagnostics, and invalid typed semantics fail closed.

### Task 4: Add semantic equivalence and analyzer-before/after evidence

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirSemanticEquivalenceService.cs`
- Create: `service-dotnet/Services/Discovery/Models/PbirSemanticEquivalenceModels.cs`
- Modify: `service-dotnet/tests/Discovery/Phase44SemanticProjectionTests.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs` only for additive evidence fields
- Modify: `service-dotnet/Services/Discovery/LocalPbirMutationProviderService.cs` to carry additive import/projection evidence through the existing backend-only mutation boundary

- [ ] **Step 1: Write equivalence tests before implementation**

Test that bindings are equivalent despite descriptor alias differences and JSON/property ordering, and are not equivalent when visual type, canonical role, kind, entity, property, token, or meaningful projection order changes. Test that unsupported untyped JSON changes do not count as semantic equivalence and are reported through fidelity instead.

- [ ] **Step 2: Implement a small IR-only equivalence service**

Compare the shared IR visuals and bindings in stable visual/order identity order. Normalize only NFC strings and descriptor-canonical roles already present in typed IR. Return unchanged semantic paths, intentional/requested paths supplied by the mutation evidence, and unexpected semantic paths. Do not parse raw imported query JSON.

- [ ] **Step 3: Add analyzer-before/after comparison tests**

Generate and import a representative multi-family report, run the existing analyzer on the imported source representation, apply a typed title/layout/binding mutation through the existing mutation pipeline, and compare analyzer outputs plus semantic equivalence. Assert unchanged semantics for unrelated visuals and an intentional delta only for the mutated visual/binding.

- [ ] **Step 4: Keep analyzer comparison advisory**

Attach comparison evidence to the existing internal mutation evidence/result contract without changing RPC contracts or making analyzer output authorize mutation. Preserve backward-compatible defaults for existing generated requests.

### Task 5: Add stage timing and fidelity evidence without optimizing

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs`
- Modify: `service-dotnet/Services/Discovery/LocalPbirMutationProviderService.cs` for import, projection, planning, execution, and merge timing boundaries
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringMergeService.cs` only if merge timing needs a narrow injectable wrapper
- Create or modify: `service-dotnet/tests/Discovery/Phase44PerformanceTests.cs`

- [ ] **Step 1: Write timing-shape and deterministic-stage tests**

Assert that representative import/mutation results expose non-negative timings for reader/import, semantic projection, merge, planning, execution, serialization, and analyzer. Assert that the report fixture and stage names are deterministic; do not assert wall-clock exact values.

- [ ] **Step 2: Add timing around existing boundaries**

Use `Stopwatch` around the already existing service calls. Keep timing additive and internal. Record semantic projection separately from file scanning/envelope capture, and authoring merge separately from serializer output creation. Do not add caching or optimize the reader in Phase 44.

- [ ] **Step 3: Verify fidelity categories against representative reports**

Use `PbirRoundTripFidelityService` and the Phase 43 envelope to report byte-identical preserved documents, authoring-identical unchanged owned documents, semantic-equivalent normalized output, intentionally changed mutation paths, and unsupported paths. Add tests that unrelated visual bindings remain unchanged after title, move, resize, and binding mutations.

### Task 6: Add representative round-trip fixtures and documentation

**Files:**
- Create: `service-dotnet/tests/Fixtures/Phase44/` fixture helpers or JSON only if generated artifacts cannot express the required query-state role
- Modify: `service-dotnet/tests/Discovery/Phase44SemanticProjectionTests.cs`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase44-semantic-binding-projection.md`
- Modify: `docs/superpowers/specs/2026-08-14-phase44-semantic-binding-projection-design.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/pbir-intermediate-representation-state.md`
- Modify: `docs/current-state/pbir-preview-serializer-state.md`
- Modify: `docs/superpowers/implementation-notes/2026-08-14-phase43-lossless-authoring.md`
- Modify: `docs/ROADMAP.md`

- [ ] **Step 1: Add one deterministic representative report per supported family**

Prefer generated artifacts with fixed input values. Include a chart Series case, Pie Legend/Category case, slicer Category case, tooltip where the serializer currently supports it, and one unsupported role/property case whose source document remains envelope-preserved.

- [ ] **Step 2: Document the semantic coverage matrix and current limitations**

Record imported roles, projected roles, unsupported roles, analyzer impact, equivalence rules, fidelity categories, hash normalization expectations, and reader/serializer asymmetries. Explicitly keep bookmarks, drillthrough, shared slicers, semantic-model/DAX generation, RPC, and new visual families out of scope.

- [ ] **Step 3: Record measured observations**

Run the representative fixture set at increasing visual counts and document observed stage timings without inventing a threshold or claiming a performance improvement.

### Task 7: Full validation and closeout

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/sessions/20260814-phase44-semantic-binding-projection.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] **Step 1: Run focused Phase 44 and Phase 42–43 regression tests**

Run the focused projection, descriptor, reader, mutation, fidelity, serializer, and analyzer tests, then the Phase 42/43 filters. Record exact pass/fail/skip counts.

- [ ] **Step 2: Run repository validation**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
dotnet build service-dotnet/PbirDesignAnalyzer.Core.csproj -c Release
cd vscode-extension && npx tsc --noEmit && npm test -- --runInBand
cd .. && git diff --check
```

Run `cd vscode-extension && npm run build` after backend validation, then restore any generated tracked backend binaries if the build updates them. Preserve expected Windows skips and record any pre-existing lint baseline separately.

- [ ] **Step 3: Audit architecture and scope**

Use `rg` to confirm there is one semantic binding record, one descriptor catalog path, no raw JSON patch API, no new RPC/VS Code surface, and no new visual family. Inspect `git diff --stat`, `git status --short`, and the final documentation for unsupported semantics and exact validation evidence.

- [ ] **Step 4: Finalize memory without committing**

Update the active session note, current focus, and session summary with completed capability, exact validation results, measured observations, known limitations, and the next recommendation. Leave all Phase 44 changes unstaged and uncommitted.
