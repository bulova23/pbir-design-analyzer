# Deterministic Modern PBIR Serializer — Repository Phase 29 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Compile canonical pbir-ir/v1 into a deterministic, schema-valid, in-memory modern PBIR file inventory and manifest without materializing or executing anything.

**Architecture:** Add a strict backend serializer downstream from pbir-serializer-request/v1. It preflights explicit model and visual bindings, builds a private modern PBIR candidate, validates the entire file set, then atomically returns versioned artifact and manifest contracts or no output.

**Tech Stack:** .NET 8, System.Text.Json, SHA-256, xUnit, pinned Microsoft PBIR Draft 7 schemas, and JsonSchema.Net 9.3.0 in the test project only. Production performs schema-lock/template validation and does not claim full Draft 7 evaluation.

---

Status: Approved and executed for Repository Phase 29 / original roadmap Phase 4A only. Phase 4B and all execution work remain unauthorized.

## File Map

Create:

- service-dotnet/Services/Discovery/Models/PbirDeployableSerializerModels.cs — versioned request, artifact, manifest, validation, readiness, diagnostics, lineage, and hash records
- service-dotnet/Services/Discovery/PbirDeployableSerializerSafetyGate.cs — IR/request/trust-boundary preflight
- service-dotnet/Services/Discovery/PbirDeployableSerializerCanonicalJson.cs — canonical JSON bytes and SHA-256 helpers scoped to this serializer
- service-dotnet/Services/Discovery/PbirIntermediateRepresentationIntegrity.cs — canonical IR content-hash recomputation at the serializer trust boundary
- service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs — subset, cross-reference, schema-lock, identity, lineage, and hash validation
- service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs — private candidate compilation and atomic state return
- service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs — deterministic, supported-subset, fail-closed, and boundary tests
- service-dotnet/tests/Discovery/PbirDeployableSerializerSchemaTests.cs — offline official-schema conformance tests
- service-dotnet/tests/Fixtures/PbirSchemas/README.md — source commit, URLs, licenses, and fixture hashes
- service-dotnet/tests/Fixtures/PbirSchemas/... — exact pinned Microsoft schemas and referenced dependencies
- docs/current-state/pbir-modern-serializer-state.md — Phase 29 current state and Phase 4A mapping

Modify:

- service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs — document the now-available deployable serializer contract without changing pbir-ir/v1
- service-dotnet/Services/Discovery/PbirIntermediateRepresentationService.cs — set serializerImplementationAvailable true after Phase 29 exists
- service-dotnet/tests/Discovery/PbirIntermediateRepresentationServiceTests.cs — update only the availability assertion and preserve non-execution checks
- service-dotnet/tests/Discovery/PbirPreviewSerializerServiceTests.cs — prove preview output and authority remain unchanged when serializerImplementationAvailable becomes true
- service-dotnet/tests/Tests.csproj — add test-only JsonSchema.Net 9.3.0 and schema fixture resources
- docs/current-state/pbir-intermediate-representation-state.md
- docs/current-state/pbir-preview-serializer-state.md
- docs/current-state/architecture-gap-analysis.md
- docs/ROADMAP.md
- docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md
- .agent-memory/current-focus.md
- .agent-memory/repo-map.md
- .agent-memory/session-summaries.md
- .agent-memory/sessions/2026-07-26T121536Z-pbir-modern-serializer-phase29-design.md

## Task 1: Add Offline Microsoft Schema Fixtures And Red Schema Tests

**Files:**

- Create: service-dotnet/tests/Fixtures/PbirSchemas/README.md
- Create: service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definitionProperties/2.0.0/schema.json
- Create: service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json
- Create: service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definition/report/1.0.0/schema.json
- Create: service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json
- Create: service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definition/page/1.0.0/schema.json
- Create: service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json
- Create referenced 1.0.0 formattingObjectDefinitions and semanticQuery schema fixtures at the same relative layout
- Modify: service-dotnet/tests/Tests.csproj
- Create: service-dotnet/tests/Discovery/PbirDeployableSerializerSchemaTests.cs

- [x] **Step 1: Vendor exact schema bytes from Microsoft commit 34356d97e1218c79331780f8f5b77b03f2d13f35.**

Record each canonical source URL, SHA-256, reviewed commit, retrieval date, and the Microsoft repository MIT license in the fixture README. Do not use main or a live URL in tests.

- [x] **Step 2: Add the test-only validator package and fixture resources.**

Add:

```xml
<PackageReference Include="JsonSchema.Net" Version="9.3.0" />
```

Keep the dependency out of PbirDesignAnalyzer.Core.csproj.

- [x] **Step 3: Write schema-lock tests before serializer implementation.**

Tests must assert:

```csharp
Assert.Equal("2.0.0", PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion);
Assert.Equal("1.0.0", PbirDeployableSchemaLock.DefinitionSchemaVersion);
Assert.Equal("4.0", PbirDeployableSchemaLock.PbirFileFormatVersion);
Assert.Equal("1.0.0", PbirDeployableSchemaLock.ReportDefinitionVersion);
```

Also add a test that loads every fixture and resolves every relative schema reference from the local registry with network resolution disabled.

- [x] **Step 4: Run the red gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableSerializerSchemaTests
```

Expected: FAIL because PbirDeployableSchemaLock and the serializer output do not exist.

- [ ] **Step 5: Commit only when the user has authorized commits.**

Proposed message:

```text
test(pbir): pin modern PBIR schema fixtures
```

## Task 2: Define Versioned Contracts And Request Preflight

**Files:**

- Create: service-dotnet/Services/Discovery/Models/PbirDeployableSerializerModels.cs
- Create: service-dotnet/Services/Discovery/PbirDeployableSerializerSafetyGate.cs
- Create: service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs

- [x] **Step 1: Write failing contract inventory and request tests.**

Cover these schema constants:

```csharp
PbirDeployableSerializerRequestContract.SchemaVersionV1
PbirDeployableArtifactContract.SchemaVersionV1
PbirDeployableManifestContract.SchemaVersionV1
PbirDeployableValidationContract.SchemaVersionV1
PbirDeployableReadinessContract.SchemaVersionV1
PbirDeployableDiagnosticsContract.SchemaVersionV1
PbirDeployableLineageContract.SchemaVersionV1
PbirDeployableHashesContract.SchemaVersionV1
```

The validation contract must expose schemaContractResults. Runtime results cover exact schema locks and supported-template structure only. PbirDeployableSerializerSchemaTests owns complete Draft 7 evaluation against local fixtures.

Lock these type names and relationships before implementation:

```text
PbirDeployableSerializerRequest
  -> PbirDatasetReference
  -> PbirSemanticModelInventory
       -> IReadOnlyList<PbirSemanticModelInventoryEntry>
  -> IReadOnlyList<PbirVisualBinding>
       -> IReadOnlyList<PbirRoleProjectionBinding>
  -> PbirDeployableExecutionPolicy

PbirDeployableSerializerState
  -> PbirDeployableArtifact?
  -> PbirDeployableManifest?
  -> PbirDeployableValidation
  -> PbirDeployableSerializerReadinessState
  -> PbirDeployableDiagnostics
```

PbirRoleProjectionBinding fields must be Role, ProjectionOrder, SourceSemanticToken, SemanticModelEntryRef, QueryRef, NativeQueryRef, Aggregation, DisplayName, and Format. PbirDeployableLineage uses DeployableSerializerRequestRef.

Create a complete request fixture with:

- modernPbir target
- safe byPath dataset reference
- locked schema versions
- modern-grid-1280x720/v1 layout
- immutable semantic-model inventory reference and content hash
- explicit measure and column inventory entries
- explicit visual role bindings with projectionOrder, sourceSemanticToken, semanticModelEntryRef, queryRef, nativeQueryRef, aggregation none, displayName null, and format null
- every execution-policy flag false

- [x] **Step 2: Write failing preflight rejection tests.**

Use member data to cover:

- wrong request or IR schema
- request/IR id or hash mismatch
- PBIR-Legacy target
- absolute, URI, drive, parent-traversal, backslash, empty-segment, and control-character paths
- unsupported layout profile
- duplicate semantic inventory entries
- missing or mismatched semantic-model inventory reference/hash
- duplicate or extra visual bindings
- any filesystem/provider/Skills/API/CLI/deployment/Desktop/Analyzer flag

Every rejection asserts:

```csharp
Assert.Null(state.Artifact);
Assert.Null(state.Manifest);
Assert.NotEqual(PbirDeployableSerializerReadinessState.Serialized, state.Readiness);
```

- [x] **Step 3: Run the red gate.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableSerializerServiceTests
```

Expected: FAIL because the Phase 29 contracts and safety gate do not exist.

- [x] **Step 4: Implement the minimum records and safety gate.**

Use immutable sealed records and IReadOnlyList collections. Do not add transport, filesystem, provider, or extension-host integration.

- [x] **Step 5: Run the focused tests.**

Expected: contract and preflight tests PASS; serialization tests remain red.

- [ ] **Step 6: Commit only when authorized.**

Proposed message:

```text
feat(pbir): define modern serializer contracts
```

## Task 3: Implement Canonical JSON, Identity, Layout, And Semantic Projection Helpers

**Files:**

- Create: service-dotnet/Services/Discovery/PbirDeployableSerializerCanonicalJson.cs
- Extend tests: service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs

- [x] **Step 1: Write failing deterministic helper assertions through the service contract.**

Assert:

- 20-character lowercase hexadecimal page and visual names
- same input produces same identities
- page and visual folder names match document name fields
- canonical content uses UTF-8, LF, one trailing LF, two-space indentation, and stable property order
- file hashes are lowercase 64-character SHA-256 values
- slot mapping is stable and nonoverlapping
- slot 1..6 geometry is exactly:

```text
1: x=24,  y=24,  width=400, height=328, z=0,    tabOrder=0
2: x=440, y=24,  width=400, height=328, z=1000, tabOrder=1000
3: x=856, y=24,  width=400, height=328, z=2000, tabOrder=2000
4: x=24,  y=368, width=400, height=328, z=3000, tabOrder=3000
5: x=440, y=368, width=400, height=328, z=4000, tabOrder=4000
6: x=856, y=368, width=400, height=328, z=5000, tabOrder=5000
```

Also assert slot 0, slot 7, duplicate slots, more than six visuals on a page, and IR order that contradicts slot order produce no artifact and no manifest.

- [x] **Step 2: Write the semantic-model inventory canonicalization tests.**

Use a literal expected byte sequence for:

```json
{"schemaVersion":"pbir-semantic-model-inventory/v1","inventoryRef":"modelInventory:sales","entries":[{"entryId":"column:Date.Month","token":"Month","entity":"Date","property":"Month","kind":"column"},{"entryId":"measure:Sales.Revenue","token":"Revenue","entity":"Sales","property":"Revenue","kind":"measure"}]}
```

Assert:

- minified UTF-8, no BOM, no whitespace, no trailing newline
- exact property order and comma/colon separators
- entries sorted by entryId, token, entity, property, kind with StringComparer.Ordinal
- NFC input requirement and UnsafeRelaxedJsonEscaping behavior
- byte length is exactly 310 and SHA-256 is exactly bc4f58184e62028614f7867e3927c5591f1b55c0104b3f70a9d85ed4e9516d29
- duplicate entryId, token, or entity/property/kind tuple is rejected before hashing
- semanticModelInventoryContentHash itself is excluded from covered bytes

- [x] **Step 3: Write failing semantic projection tests.**

Create representable IR fixtures for:

- card with one measure
- table with columns and measures
- clusteredColumnChart with one category and measures
- lineChart with one category and measures

Verify:

- card emits only Fields with exactly one direct Measure
- table emits only Values with direct Column and Measure projections in explicit projectionOrder
- clusteredColumnChart emits Category with exactly one direct Column and Y with direct Measures
- lineChart emits Category with exactly one direct Column and Y with direct Measures
- Entity and Property come only from the referenced inventory entry
- queryRef and nativeQueryRef come only from the binding
- aggregation must be explicitly none
- displayName and format must be explicitly null and are omitted
- auto, a missing role, an extra role, implicit aggregation, synthesized queryRef, or guessed table is rejected

- [x] **Step 4: Implement serializer-scoped deterministic helpers.**

Use domain-separated hashes:

```text
page|[irId]|[pageIdentity]
visual|[irId]|[pageIdentity]|[visualId]
```

Use the fixed 1280×720 six-slot grid above and reject slot reuse, overflow, or contradictory order.

- [x] **Step 5: Run the focused tests.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirDeployableSerializerServiceTests
```

Expected: helper and projection tests PASS.

- [ ] **Step 6: Commit only when authorized.**

Proposed message:

```text
feat(pbir): add deterministic PBIR projections
```

## Task 4: Build The Atomic In-Memory Modern PBIR Candidate

**Files:**

- Create: service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs
- Extend tests: service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs

- [x] **Step 1: Write the failing coherent-inventory test.**

Assert the exact minimum:

```text
definition.pbir
definition/version.json
definition/report.json
definition/pages/pages.json
definition/pages/[page]/page.json
definition/pages/[page]/visuals/[visual]/visual.json
```

Assert the inventory does not contain:

```text
report.json
*.pbip
model.bim
definition.pbism
.platform
```

- [x] **Step 2: Write the failing determinism and navigation tests.**

Serialize the same IR/request twice and compare:

- exact file content strings
- exact serialized artifact and manifest
- exact per-file, file-set, artifact, manifest, input, and lineage hashes
- page order and active page
- sequential navigation assertions

- [x] **Step 3: Write exact canonical document baseline tests.**

Use literal expected JSON strings, including two-space indentation and one trailing LF, for every template defined in the approved design:

- definition.pbir
- definition/version.json
- definition/report.json
- definition/pages/pages.json
- page.json
- card visual.json
- table visual.json
- clusteredColumnChart visual.json
- lineChart visual.json

Assert exact property order. Assert definition/report.json contains layoutOptimization None and an empty themeCollection; it must not infer theme, formatting, resources, filters, settings, or annotations. Assert page displayName is copied from IR Page.PageId, and visual roles/fields/queryRef/nativeQueryRef are copied only from validated source values.

- [x] **Step 4: Implement candidate creation privately.**

The service method shape is:

```csharp
internal PbirDeployableSerializerState CreateArtifacts(
    PbirIntermediateRepresentationState irState,
    PbirSerializerRequest serializerRequest,
    PbirDeployableSerializerRequest request)
```

Do not accept an output directory, file service, provider, client, process runner, or clock.

- [x] **Step 5: Keep candidate output private until validation succeeds.**

On any preflight or postflight diagnostic:

```csharp
return new PbirDeployableSerializerState(
    Artifact: null,
    Manifest: null,
    Validation: validation,
    Readiness: readiness,
    Diagnostics: diagnostics);
```

- [x] **Step 6: Run the focused tests.**

Expected: coherent-inventory, navigation, and determinism tests PASS.

- [ ] **Step 7: Commit only when authorized.**

Proposed message:

```text
feat(pbir): serialize modern PBIR in memory
```

## Task 5: Add Postflight Validation And Fail-Closed Coverage

**Files:**

- Create: service-dotnet/Services/Discovery/PbirDeployableSerializerValidator.cs
- Extend tests: service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs
- Extend tests: service-dotnet/tests/Discovery/PbirDeployableSerializerSchemaTests.cs

- [x] **Step 1: Write failing negative tests for unsupported IR.**

Cover:

- unsupported or custom visual
- missing, ambiguous, wrong-kind, duplicate, or unused binding
- invalid model entity/property reference
- auto dimension
- implicit aggregation request
- unsupported filters, drill behavior, relationship, interaction, bookmark, or responsive/mobile intent
- duplicate page, visual, container, slot, order, or generated identity
- invalid landing page or nonsequential transition

- [x] **Step 2: Write failing tamper tests.**

Mutate one field at a time:

- relative path
- schema URL
- JSON content
- byte length
- file hash
- file-set hash
- artifact hash
- manifest hash
- immutable lineage
- lineage hash
- generated-file sourceIrReferences
- schema lock
- supported features
- warnings
- unsupported sections

Each validation must identify the stable diagnostic code and must not produce a successful state.

- [x] **Step 3: Implement deterministic postflight validation.**

Validate:

- exact schema allowlist
- schemaContractResults for exact supported-template URLs, required properties, types, and forbidden properties
- exact required inventory
- no root report.json
- path normalization and uniqueness
- JSON parseability and required subset fields
- page/visual folder-name and object-name equality
- active page, page order, and visual references
- semantic inventory and projection consistency
- exact inventory-entry, semantic-token, and relationship coverage
- current canonical IR content hash rather than trusting stale validation state
- hashes and lineage

- [x] **Step 4: Validate every emitted file against local official fixtures.**

PbirDeployableSerializerSchemaTests registers local fixture ids and evaluates exact emitted JSON bytes without network resolution.

This is the complete Draft 7 conformance guarantee. Do not copy these results into the runtime schemaContractResults field and do not add a runtime field that claims full JSON Schema evaluation.

- [x] **Step 5: Run both focused suites.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirDeployableSerializerServiceTests|FullyQualifiedName~PbirDeployableSerializerSchemaTests"
```

Expected: PASS.

- [ ] **Step 6: Commit only when authorized.**

Proposed message:

```text
test(pbir): enforce modern serializer safety
```

## Task 6: Prove The Trust Boundary And Activate The Existing Serializer Flag

**Files:**

- Modify: service-dotnet/Services/Discovery/PbirIntermediateRepresentationService.cs
- Modify: service-dotnet/tests/Discovery/PbirIntermediateRepresentationServiceTests.cs
- Modify: service-dotnet/tests/Discovery/PbirPreviewSerializerServiceTests.cs
- Extend tests: service-dotnet/tests/Discovery/PbirDeployableSerializerServiceTests.cs

- [x] **Step 1: Add exact callable-surface reflection tests.**

Assert the service entry point has exactly this callable shape:

```csharp
internal PbirDeployableSerializerState CreateArtifacts(
    PbirIntermediateRepresentationState irState,
    PbirSerializerRequest serializerRequest,
    PbirDeployableSerializerRequest request)
```

Assert constructor and instance-field dependency types are limited to:

```text
PbirDeployableSerializerSafetyGate
PbirDeployableSerializerValidator
PbirDeployableSerializerCanonicalJson
```

Assert no declared serializer constructor, field, parameter, or return type is FileInfo, DirectoryInfo, FileSystemInfo, Stream, HttpClient, Process, IRuntimeProvider, IReferenceGenerationProvider, a writer service/interface, an execution-provider service/interface, or an Analyzer/Design Studio service type.

- [x] **Step 2: Add precise dependency and authority checks.**

Use the callable-surface and constructor/field dependency reflection checks above, plus project-reference checks proving the core project gains no provider, CLI, HTTP, Desktop, production schema-validator, or extension-host package/project reference. Exercise every execution-policy flag independently and require each positive authority value to fail closed.

Do not scan source for broad or incidental tokens. The required fields providerInvocationAllowed, apiInvocationAllowed, cliInvocationAllowed, deploymentAllowed, desktopAutomationAllowed, analyzerAutomationAllowed, and deployableSerializerRequestRef must compile and remain covered by negative-authority tests.

- [x] **Step 3: Change serializerImplementationAvailable to true.**

Update the existing request test to assert true while retaining false provider, deployment, and Microsoft Skills authority.

- [x] **Step 4: Add the preview serializer regression test.**

Create the existing safe preview input after serializerImplementationAvailable becomes true, run PbirPreviewSerializerService.CreatePreviewArtifacts, and assert:

- output and manifest remain byte-identical to the existing preview baseline for the same inputs and generatedUtc
- preview readiness remains Generated
- providerInvocationAllowed, deploymentAllowed, and microsoftSkillsExecutionAllowed remain false
- generated preview files remain only under pbir-preview-artifact/v1
- no definition.pbir, definition/report.json, root report.json, or definition/ page/visual artifact appears
- deployable-output options are still rejected by PbirPreviewSerializerSafetyGate
- the preview service callable surface gains no PbirDeployableSerializerService dependency or deployable return type

- [x] **Step 5: Run focused boundary and preview regression tests.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirDeployableSerializerServiceTests|FullyQualifiedName~PbirIntermediateRepresentationServiceTests|FullyQualifiedName~PbirPreviewSerializerServiceTests"
```

Expected: PASS.

- [ ] **Step 6: Commit only when authorized.**

Proposed message:

```text
feat(pbir): activate in-memory modern serializer
```

## Task 7: Update Current State, Architecture Gaps, Roadmap Mapping, And Memory

**Files:**

- Create: docs/current-state/pbir-modern-serializer-state.md
- Modify: docs/current-state/pbir-intermediate-representation-state.md
- Modify: docs/current-state/pbir-preview-serializer-state.md
- Modify: docs/current-state/architecture-gap-analysis.md
- Modify: docs/ROADMAP.md
- Modify: docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md
- Modify: .agent-memory/current-focus.md
- Modify: .agent-memory/repo-map.md
- Modify: .agent-memory/session-summaries.md
- Modify: .agent-memory/sessions/2026-07-26T121536Z-pbir-modern-serializer-phase29-design.md

- [x] **Step 1: Document the exact delivered boundary.**

State:

- Repository Phase 29 equals original Phase 4A serialization only
- modern PBIR only
- definition/report.json is modern and root report.json remains forbidden
- output is an in-memory artifact inventory
- current upstream IR with unresolved auto semantics may be rejected
- no filesystem or execution surface exists

- [x] **Step 2: Name the next phase exactly.**

Use:

**Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**

State that it requires a new goal and must not reuse or widen the preview-only writer.

- [x] **Step 3: Preserve roadmap truth.**

Update original Phase 4 with explicit subphase status:

- Phase 4A: Repository Phase 29
- Phase 4B: next and not started
- provider/execution/deployment work: not started

- [x] **Step 4: Run documentation assertions.**

Run targeted rg checks for:

```text
Phase 29
original Phase 4A
definition/report.json
root-level report.json
in-memory
Safe Local Deployable PBIR Materialization
preview/apply/rollback
```

- [ ] **Step 5: Commit only when authorized.**

Proposed message:

```text
docs: map modern serializer to roadmap phase 4A
```

## Task 8: Run Focused And Full Validation, Record Actual Counts, Then Stop

**Files:**

- Modify: .agent-memory/current-focus.md
- Modify: .agent-memory/session-summaries.md
- Modify: .agent-memory/sessions/2026-07-26T121536Z-pbir-modern-serializer-phase29-design.md

- [x] **Step 1: Run the final focused backend gate.**

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirDeployableSerializer|FullyQualifiedName~PbirIntermediateRepresentationServiceTests"
```

Expected: PASS with an actual count recorded.

- [x] **Step 2: Run the full backend suite once.**

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

Expected: PASS. Record passed, failed, skipped, and total counts from the command output.

- [x] **Step 3: Run all Jest suites once.**

```bash
cd vscode-extension && npm test
```

Expected: PASS. Record test suite and test counts from the command output.

- [x] **Step 4: Run TypeScript compilation once.**

```bash
cd vscode-extension && npm run compile
```

Expected: exit code 0. Record the result.

- [x] **Step 5: Inspect final diff and status.**

Confirm:

- no unrelated change was reset, amended, absorbed, or discarded
- no filesystem writer or execution integration was added
- no root report.json is generated
- no Phase 4B work began

- [x] **Step 6: Finalize memory.**

Record exact validation counts, files delivered, known unsupported subset, and next phase boundary.

- [ ] **Step 7: Commit only when authorized.**

Proposed final message:

```text
feat(pbir): complete deterministic modern serializer phase 29
```

- [x] **Step 8: Stop.**

Do not begin safe local materialization, provider execution, Microsoft Skills execution, Desktop automation, deployment, publishing, Analyzer automation, refinement loops, Fabric App generation, or Fabric Data App generation.

## Downstream Phase 31 Note

The separately authorized Repository Phase 31 application boundary reuses PbirDeployableSerializerService directly and leaves every Phase 29 contract and supported-subset decision unchanged.
