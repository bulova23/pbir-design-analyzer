# Phase 43 Lossless Authoring IR Implementation Plan

> **For agentic workers:** Execute this plan task-by-task with test-first checkpoints. Phase 43 must remain uncommitted and unstaged; do not add commit steps.

**Goal:** Preserve schema-admitted imported PBIR authoring state through typed mutation and deterministic serialization without expanding every PBIR field into typed IR.

**Architecture:** Add a bounded authoring envelope to the existing shared IR and import snapshot. The reader captures owned JSON documents and typed identity projections; a focused merge service applies validated typed changes to copies of those documents; the serializer emits the resolved representation and continues its existing typed-only generation path when no imported envelope exists. Unsupported schema content fails closed.

**Tech Stack:** .NET 8, C#, System.Text.Json, xUnit, existing PBIR schema lock and deployable serializer.

---

### Task 1: Establish the authoring envelope contracts and preservation matrix

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs`
- Modify: `service-dotnet/Services/Discovery/Models/PbirLocalReportImportModels.cs`
- Create: `service-dotnet/Services/Discovery/Models/PbirAuthoringEnvelopeModels.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringEnvelopeContractTests.cs`
- Modify: `docs/current-state/pbir-intermediate-representation-state.md`

- [ ] Write failing contract tests proving envelope item classifications, imported/generated/override identity fields, owned relative paths, source hashes, and JSON subtree serialization are explicit and do not expose arbitrary patch operations.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringEnvelopeContractTests`; expect compilation failure for missing envelope types.
- [ ] Add immutable records for envelope ownership, item classification, identity provenance, source ordering, and bounded JSON documents. Use `JsonElement` clones or UTF-8 JSON bytes so documents remain detached from disposed `JsonDocument` instances.
- [ ] Add optional envelope and fidelity metadata to the IR/import snapshot in a backward-compatible way. Keep existing constructor call sites compiling through optional parameters or targeted updates, and do not change generation-only serialized output.
- [ ] Add tests for `TypedSupported`, `OpaquePreserved`, and `Unsupported`, for imported identity selection, and for rejection of a raw JSON mutation field/API.
- [ ] Update the current-state IR document with the preservation matrix and the exact distinction between typed, opaque, and unsupported content.
- [ ] Run the focused contract tests and the existing IR contract tests; expected result is green.

### Task 2: Capture the bounded imported authoring envelope

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirLocalReportReader.cs`
- Modify: `service-dotnet/Services/Discovery/Models/PbirDeployableSerializerModels.cs`
- Create: `service-dotnet/Services/Discovery/PbirAuthoringEnvelopeReader.cs`
- Test: `service-dotnet/tests/Discovery/PbirLocalReportReaderTests.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringEnvelopeReaderTests.cs`

- [ ] Add fixture-based failing tests for report, pages metadata, report, page, and visual documents containing formatting, theme, filter, navigation, slicer, and unknown schema-supported properties.
- [ ] Run only the new reader tests and verify failure because the import snapshot has no populated envelope and no typed imported identity provenance.
- [ ] Implement `PbirAuthoringEnvelopeReader` with one responsibility: admit only known schema-lock URLs and known owned definition paths, clone the original JSON document, record source hash and property ordering, and emit fail-closed diagnostics for invalid/unsupported schema content.
- [ ] Update `PbirLocalReportReader` to use that service, reuse captured page/visual folder identities, and avoid synthesizing imported defaults when a source property exists.
- [ ] Preserve deterministic directory/file traversal ordering while retaining source order metadata; do not store arbitrary non-definition files.
- [ ] Add tests proving themes, filters, page/visual formatting, navigation metadata, slicer metadata, and schema-supported unknown properties survive import; unsupported schemas and unsupported constructs produce diagnostics and blocked readiness.
- [ ] Run focused reader tests and the Phase 42 import regression tests.

### Task 3: Add typed identity provenance and deterministic allocation

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs`
- Create: `service-dotnet/Services/Discovery/PbirAuthoringIdentityResolver.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationPlanner.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationExecutor.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringIdentityTests.cs`

- [ ] Write failing tests for unchanged imported page/visual folder identities, explicit typed identity overrides, collision rejection, and deterministic identities for newly added pages/visuals.
- [ ] Run the identity tests and confirm the current serializer derives identities from the IR ID and fails the imported identity assertions.
- [ ] Implement a resolver that chooses imported identity, then explicit validated override, then the existing deterministic generated provider identity according to object provenance and mutation state.
- [ ] Extend typed page/visual identity state without changing existing generation defaults. Validate that identity overrides remain within their owning object and are unique across the resolved report.
- [ ] Update planner/executor to retain imported ownership for unchanged objects and mark only additions/explicit identity changes as regenerated or changed.
- [ ] Update serializer path construction and references to consume resolved identities rather than deriving all folder names from `ir.Metadata.IrId`.
- [ ] Run identity tests, generation determinism tests, and Phase 42 mutation tests.

### Task 4: Implement the single authoring merge boundary

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirAuthoringMergeService.cs`
- Create: `service-dotnet/Services/Discovery/Models/PbirResolvedAuthoringModels.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationExecutor.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationPlanning.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringMergeServiceTests.cs`

- [ ] Write failing tests for merge precedence: untouched source property wins over synthesized typed default; a typed mutation wins only for its modeled property; unrelated opaque properties remain byte/semantic-equivalent; unsupported ownership blocks the merge.
- [ ] Run the merge tests and confirm the service does not exist.
- [ ] Implement resolved authoring models containing the resolved file set, object ownership, selected identity, typed IR, and fidelity change paths.
- [ ] Implement merge operations for the current typed mutation set: page/visual add/remove/move/resize, binding changes, visual formatting, theme, filter, navigation, and slicer changes only where the existing request models can represent them.
- [ ] Use `JsonNode`/`JsonObject` or equivalent cloned DOM operations only inside this service; expose no general patch method and accept no caller-supplied JSON paths or replacement subtrees.
- [ ] Ensure missing typed support produces a diagnostic rather than deleting or replacing the opaque subtree.
- [ ] Recompute IR content hashes from the resolved typed state and preserve source file hashes separately for fidelity reporting.
- [ ] Run merge tests and mutation planner/executor tests.

### Task 5: Integrate resolved authoring output into the deployable serializer

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs`
- Modify: `service-dotnet/Services/Discovery/PbirDeployableSerializerModels.cs`
- Test: `service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs`
- Test: `service-dotnet/tests/Discovery/PbirDeployableSerializerSchemaTests.cs`

- [ ] Add failing round-trip tests that import a representative report, serialize without mutation, and compare source/output identity, formatting, theme, filters, navigation, slicer metadata, layouts, and analyzer-relevant bindings.
- [ ] Run the focused serializer tests and record the current expected failures caused by regenerated identities and dropped properties.
- [ ] Make the serializer consume resolved authoring files from the merge service when present. Keep the current writer methods as the generation fallback for IR states with no envelope.
- [ ] Emit preserved source documents with canonical UTF-8 output only where normalization is required; preserve original bytes when no typed field changed and the artifact contract permits it.
- [ ] For changed documents, merge typed properties into the original object and preserve unrelated properties/order where safely possible; do not reconstruct untouched pages/visuals from scratch.
- [ ] Validate the resolved output with the existing schema validator after merge and before `Serialized` readiness.
- [ ] Add schema tests for the emitted preserved report, page, visual, filter, theme, navigation, and slicer shapes; unsupported shapes must remain blocked.
- [ ] Run focused serializer/schema tests and the complete existing deployable serializer suite.

### Task 6: Extend mutation evidence, fidelity comparison, and analyzer delta

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirRoundTripFidelityService.cs`
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs`
- Modify: `service-dotnet/Services/Discovery/LocalPbirMutationProviderService.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationExecutor.cs`
- Test: `service-dotnet/tests/Discovery/PbirRoundTripFidelityServiceTests.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirMutationProviderServiceTests.cs`

- [ ] Write failing tests for byte-identical, semantically identical, expected normalized, and unexpected file differences; test preserved/changed identity and authoring paths.
- [ ] Run the tests and verify the comparison service is absent and mutation evidence has no fidelity fields.
- [ ] Implement a read-only fidelity comparer that canonicalizes JSON for semantic comparison, compares source/output hashes, classifies expected normalization, and blocks unexpected unrelated changes.
- [ ] Extend mutation evidence additively with preserved/changed identity lists, authoring preservation paths, hash delta categories, analyzer-before/after result, and performance timings. Keep existing JSON property names and defaults compatible.
- [ ] Run the analyzer on imported source and resolved output using the existing analyzer service; keep analyzer output evidence-only and preserve score authority.
- [ ] Add tests demonstrating an unchanged report has no analyzer delta, a single visual title/format mutation changes only intended findings, and unrelated findings remain preserved.
- [ ] Run focused fidelity/evidence tests and existing provider tests.

### Task 7: Add representative golden fixtures and unsupported-content coverage

**Files:**
- Create: `service-dotnet/tests/Fixtures/Phase43/representative-report/definition/...`
- Create: `service-dotnet/tests/Fixtures/Phase43/unsupported-.../definition/...`
- Create: `service-dotnet/tests/Discovery/Phase43RoundTripFixtureTests.cs`
- Modify: `service-dotnet/tests/Fixtures/PbirSchemas/README.md`

- [ ] Add small deterministic fixtures covering one report theme, report/page/visual filters, page formatting, card/table/chart formatting, navigation metadata, and slicer metadata without introducing unsupported constructs.
- [ ] Add an unsupported fixture with a schema URL or construct outside the pinned inventory and assert fail-closed diagnostics.
- [ ] Write golden tests that compare selected canonical JSON paths and complete file hash classifications rather than asserting brittle whole-file equality where normalization is intentional.
- [ ] Verify newly added objects use deterministic identities and do not reuse imported folder names.
- [ ] Run the fixture tests twice and assert deterministic results.

### Task 8: Measure performance and complete documentation

**Files:**
- Create: `docs/current-state/phase43-lossless-authoring-state.md`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase43-lossless-authoring.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/architecture/phase42-report-mutation.md`
- Modify: `docs/current-state/pbir-modern-serializer-state.md`
- Modify: `docs/current-state/pbir-intermediate-representation-state.md`
- Modify: `docs/superpowers/specs/2026-08-14-phase42-report-mutation-design.md`

- [ ] Add a deterministic performance harness/test measurement for import, planning, execution, serialization, and analyzer stages, recording observations against the Phase 42 baseline without inventing a threshold.
- [ ] Document the final preservation matrix, supported PBIR subset, fidelity categories, identity behavior, representative round-trip results, analyzer comparison, hash explanations, performance observations, and remaining limitations.
- [ ] Update roadmap status to show Phase 43 complete only if the required fidelity gates pass; otherwise document the exact blocked gate and do not recommend Phase 44 RPC.
- [ ] Record that Phase 44 may evaluate a minimal internal RPC surface only after backend authoring fidelity is demonstrated; do not implement RPC.
- [ ] Run placeholder/documentation scans and `git diff --check`.

### Task 9: Full validation and uncommitted closeout

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-14-phase43-lossless-authoring.md`

- [ ] Run focused Phase 43 backend tests.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release` and record exact counts/skips.
- [ ] Run `dotnet build service-dotnet/PbirDesignAnalyzer.sln -c Release` or the repository’s authoritative .NET build command and record the result.
- [ ] Run extension TypeScript compilation, extension Jest tests, webview tests, extension build, and packaging according to `AGENTS.md`; preserve expected Windows skips.
- [ ] Run `git diff --check`, inspect `git status --short`, and verify every changed file is intentional, unstaged, and uncommitted.
- [ ] Finalize the session note and update current focus with the next safe step, including any validation that could not run.

