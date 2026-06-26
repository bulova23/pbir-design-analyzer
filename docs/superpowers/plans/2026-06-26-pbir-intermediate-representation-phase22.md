# PBIR Intermediate Representation Phase 22 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add canonical pbir-ir/v1 as the deterministic handoff contract between generation-manifest/v1, PBIR generation specification, and future generation providers.

**Architecture:** The IR layer lives in service-dotnet/Services/Discovery beside the existing planning and reference generator layers. It consumes only GenerationManifestState and PbirGenerationSpecificationState, produces canonical IR state, validates/readies it, and exposes only a serializer request contract with no serializer implementation.

**Tech Stack:** .NET 8, C# records, xUnit.

---

### Task 1: Canonical IR Tests

**Files:**
- Create: `service-dotnet/tests/Discovery/PbirIntermediateRepresentationServiceTests.cs`

- [ ] Write failing tests for deterministic IR generation, stable ordering, immutable IDs, invalid layout, invalid semantics, invalid navigation, incomplete IR, serializer request contract, and boundary protection.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirIntermediateRepresentationServiceTests` and verify the tests fail because Phase 22 types do not exist.

### Task 2: IR Contracts And Services

**Files:**
- Create: `service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs`
- Create: `service-dotnet/Services/Discovery/PbirIntermediateRepresentationService.cs`
- Create: `service-dotnet/Services/Discovery/PbirIntermediateRepresentationValidator.cs`
- Create: `service-dotnet/Services/Discovery/PbirIntermediateRepresentationReadinessService.cs`

- [ ] Implement pbir-ir/v1 metadata, references, page IR, visual IR, semantic IR, navigation IR, layout IR, success criteria, lineage, and hashes.
- [ ] Implement pbir-serializer-request/v1 as a request contract only.
- [ ] Implement deterministic mapping from generation manifest and PBIR generation specification into canonical IR.
- [ ] Implement validation for completeness, navigation integrity, semantic integrity, layout integrity, schema compatibility, and non-execution boundaries.
- [ ] Implement readiness states incomplete, blocked, canonical, and readyForSerializer.
- [ ] Run the focused test filter and verify green.

### Task 3: Reference Generator Integration

**Files:**
- Modify: `service-dotnet/Services/Discovery/ReferencePbirGenerationService.cs`
- Modify: `service-dotnet/Services/Discovery/Models/ReferencePbirGenerationModels.cs`
- Modify: `service-dotnet/tests/Discovery/ReferencePbirGenerationServiceTests.cs`

- [ ] Update reference-generation-output/v1 to include canonical IR references and deterministic IR hashes.
- [ ] Replace descriptive PBIR intermediate output with canonical pbir-ir/v1 content.
- [ ] Preserve local-only descriptors and immutable lineage.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirIntermediateRepresentationServiceTests|FullyQualifiedName~ReferencePbirGenerationServiceTests"` and verify green.

### Task 4: Documentation And Memory

**Files:**
- Create: `docs/current-state/pbir-intermediate-representation-state.md`
- Modify: `docs/current-state/reference-generator-state.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Modify: `.agent-memory/sessions/2026-06-26-pbir-intermediate-representation-phase22.md`

- [ ] Document pbir-ir/v1, IR schema, canonical mapping rules, serializer boundary, IR lifecycle, and remaining serializer implementation gap.
- [ ] Record that the reference generator now emits canonical IR and deterministic IR hashes.
- [ ] Preserve the explicit stop boundary after Phase 22.

### Task 5: Required Validation

- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release`.
- [ ] Run `cd vscode-extension && npm test`.
- [ ] Run `cd vscode-extension && npm run compile`.
- [ ] Record validation results in session memory.
